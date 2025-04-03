using IqraCore.Entities.Business;
using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.Conversation;
using IqraCore.Models.Server;
using IqraCore.Models.Telephony;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Conversation;
using IqraInfrastructure.Managers.Conversation.Client;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.Script;
using IqraInfrastructure.Managers.Server;
using IqraInfrastructure.Managers.STT;
using IqraInfrastructure.Managers.Telephony;
using IqraInfrastructure.Managers.TTS;
using IqraInfrastructure.Repositories.Conversation;
using IqraInfrastructure.Repositories.Telephony;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace IqraInfrastructure.Managers.Call
{
    public class CallProcessorManager
    {
        private readonly ILogger<CallProcessorManager> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ServerStatusManager _serverStatusManager;
        private readonly CallQueueRepository _callQueueRepository;
        private readonly ConversationStateRepository _conversationStateRepository;
        private readonly BusinessManager _businessManager;
        private readonly IntegrationsManager _integrationsManager;

        private readonly ConcurrentDictionary<string, ConversationSessionManager> _activeSessions = new();
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _sessionCancellationTokens = new();
        private readonly SemaphoreSlim _sessionCreationLock = new SemaphoreSlim(1, 1);

        public CallProcessorManager(
            ILogger<CallProcessorManager> logger,
            IServiceProvider serviceProvider,
            ServerStatusManager serverStatusService,
            CallQueueRepository callQueueRepository,
            ConversationStateRepository conversationStateRepository,
            BusinessManager businessManager,
            IntegrationsManager integrationsManager)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _serverStatusManager = serverStatusService;
            _callQueueRepository = callQueueRepository;
            _conversationStateRepository = conversationStateRepository;
            _businessManager = businessManager;
            _integrationsManager = integrationsManager;
        }

        public async Task<string> CreateConversationSessionAsync(
            ConversationSessionConfiguration config,
            TelephonyWebhookContextModel clientData,
            CancellationToken cancellationToken)
        {
            // Check if server has capacity
            if (!_serverStatusManager.HasCapacity())
            {
                _logger.LogWarning("Server at capacity, cannot create new conversation session");
                return string.Empty;
            }

            // Create a unique session ID
            string sessionId = Guid.NewGuid().ToString();

            try
            {
                await _sessionCreationLock.WaitAsync(cancellationToken);    
                await _callQueueRepository.UpdateCallSessionIdAndStatusAsync(config.QueueId, sessionId, CallQueueStatusEnum.Processing);

                // Double check capacity after acquiring lock
                if (!_serverStatusManager.HasCapacity())
                {
                    _logger.LogWarning("Server at capacity after lock, cannot create new conversation session");
                    return string.Empty;
                }

                // Create a cancellation token for this session
                var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _sessionCancellationTokens[sessionId] = sessionCts;

                // Create conversation session
                var conversationSession = new ConversationSessionManager(
                    sessionId,
                    _businessManager,
                    config,
                    _conversationStateRepository,
                    _serviceProvider.GetRequiredService<ConversationAudioRepository>(),
                    _serviceProvider.GetRequiredService<ILogger<ConversationSessionManager>>(),
                    "call"
                );

                // Create telephony client based on provider
                var telephonyClient = await CreateTelephonyClient(clientData, sessionId);
                if (!telephonyClient.Success || telephonyClient.Data == null)
                {
                    _logger.LogWarning("Failed to create telephony client for session {SessionId} with message: {Message}", sessionId, telephonyClient.Message);
                    await CleanupSessionAsync(sessionId);
                    return string.Empty;
                }

                // Add client to session
                await conversationSession.AddClientAsync(telephonyClient.Data);
                conversationSession.SetPrimaryClient(telephonyClient.Data.ClientId);

                // Create and add AI agent
                var agent = await CreateAIAgentAsync(sessionId, config, conversationSession);
                if (agent != null)
                {
                    await conversationSession.AddAgentAsync(agent, new ConversationAgentConfiguration
                    {
                        BusinessId = config.BusinessId,
                        RouteId = config.RouteId
                    });
                    conversationSession.SetPrimaryAgent(agent.AgentId);
                }

                // Start the session
                _ = conversationSession.StartAsync(sessionCts.Token);

                // Store in active sessions
                _activeSessions[sessionId] = conversationSession;

                // Update server status
                await _callQueueRepository.UpdateStatusAsync(config.QueueId, CallQueueStatusEnum.Completed);
                _serverStatusManager.IncrementActiveCalls();

                _logger.LogInformation("Created conversation session {SessionId} for business {BusinessId}",
                    sessionId, config.BusinessId);

                return sessionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating conversation session");
                await CleanupSessionAsync(sessionId);
                // todo make sure conversation session is completely disposed
                return string.Empty;
            }
            finally
            {
                _sessionCreationLock.Release();
            }
        }

        public async Task<OutboundCallResultModel> InitiateOutboundCallAsync(
            long businessId,
            string phoneNumberId,
            string toNumber,
            string queueId,
            string routeId)
        {
            var result = new OutboundCallResultModel();

            try
            {
                // Get the business number
                var businessNumber = await _businessManager.GetNumberManager().GetBusinessNumberById(businessId, phoneNumberId);
                if (businessNumber == null)
                {
                    result.Message = "Business number not found";
                    return result;
                }

                // Get integration data for the provider
                var integrationResult = await _businessManager.GetIntegrationsManager()
                    .getBusinessIntegrationById(businessId, businessNumber.IntegrationId);

                if (!integrationResult.Success || integrationResult.Data == null)
                {
                    result.Message = $"Integration not found: {integrationResult.Message}";
                    return result;
                }

                // Initialize the call based on provider
                switch (businessNumber.Provider)
                {
                    case TelephonyProviderEnum.ModemTel:
                        return await InitiateModemTelOutboundCallAsync(
                            businessId,
                            businessNumber as BusinessNumberModemTelData,
                            integrationResult.Data,
                            toNumber,
                            queueId);

                    case TelephonyProviderEnum.Twilio:
                        return await InitiateTwilioOutboundCallAsync(
                            businessId,
                            businessNumber as BusinessNumberTwilioData,
                            integrationResult.Data,
                            toNumber,
                            queueId);

                    default:
                        result.Message = $"Unsupported provider: {businessNumber.Provider}";
                        return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating outbound call");
                result.Message = $"Error initiating call: {ex.Message}";
                return result;
            }
        }

        public async Task EndConversationSessionAsync(string sessionId, string reason)
        {
            if (_activeSessions.TryGetValue(sessionId, out var session))
            {
                try
                {
                    await session.EndAsync(reason);

                    // Update server status
                    _serverStatusManager.DecrementActiveCalls();

                    _logger.LogInformation("Ended conversation session {SessionId}: {Reason}", sessionId, reason);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error ending conversation session {SessionId}", sessionId);
                }
                finally
                {
                    await CleanupSessionAsync(sessionId);
                }
            }
            else
            {
                _logger.LogWarning("Session {SessionId} not found for ending", sessionId);
            }
        }

        public async Task EndClientConnectionFromConversation(string sessionId, string reason, TelephonyProviderEnum provider, string phoneNumberId)
        {
            if (_activeSessions.TryGetValue(sessionId, out var session))
            {
                try
                {
                    string clientId = $"{provider}_{phoneNumberId}";
                    await session.RemoveClientAsync(clientId, reason);

                    _logger.LogInformation("Ended client connection conversation session {SessionId}: {Reason}", sessionId, reason);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error ending client connection conversation session {SessionId}", sessionId);
                }
                finally
                {
                    await CleanupSessionAsync(sessionId);
                }
            }
            else
            {
                _logger.LogWarning("Session {SessionId} not found for ending client connection", sessionId);
            }
        }
        private async Task CleanupSessionAsync(string sessionId)
        {
            // Remove from active sessions
            _activeSessions.TryRemove(sessionId, out _);

            // Cancel and dispose token
            if (_sessionCancellationTokens.TryRemove(sessionId, out var cts))
            {
                try
                {
                    cts.Cancel();
                    cts.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing session cancellation token");
                }
            }
        }

        private async Task<FunctionReturnResult<IConversationClient?>> CreateTelephonyClient(TelephonyWebhookContextModel clientData, string sessionId)
        {
            var result = new FunctionReturnResult<IConversationClient?>();

            // Create a client ID from session ID and provider
            string clientId = $"{clientData.Provider}_{clientData.PhoneNumberId}";

            var businessNumberData = await _businessManager.GetNumberManager().GetBusinessNumberById(clientData.BusinessId, clientData.PhoneNumberId);
            if (businessNumberData == null)
            {
                result.Message = "Business number not found";
                _logger.LogError("Business number not found for business {BusinessId}", clientData.BusinessId);
                return result;
            }
            var integrationData = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(clientData.BusinessId, businessNumberData.IntegrationId);
            if (!integrationData.Success || integrationData.Data == null)
            {
                result.Message = "Integration not found";
                _logger.LogError("Integration not found for business {BusinessId}", clientData.BusinessId);
                return result;
            }

            string phoneNumberData = businessNumberData.CountryCode + businessNumberData.Number; // todo this is alphabet country code not number

            switch (clientData.Provider)
            {
                case TelephonyProviderEnum.ModemTel:
                    // Get required ModemTel data
                    var token = clientData.AdditionalData["mediaSessionToken"];

                    result.Success = true;
                    result.Data = new ModemTelConversationClient(
                        clientId,
                        phoneNumberData,
                        clientData.CallId,
                        integrationData.Data.Fields["endpoint"],
                        _integrationsManager.DecryptField(integrationData.Data.EncryptedFields["apikey"]),
                        token,
                        _serviceProvider.GetRequiredService<ModemTelManager>(),
                        _serviceProvider.GetRequiredService<ILogger<ModemTelConversationClient>>());

                    return result;

                case TelephonyProviderEnum.Twilio:
                    // Get required Twilio data
                    var accountSid = clientData.AdditionalData["accountSid"];
                    var callbackUrl = clientData.AdditionalData["callbackUrl"];

                    result.Success = true;
                    result.Data = new TwilioConversationClient(
                        clientId,
                        phoneNumberData,
                        clientData.CallId,
                        accountSid,
                        _integrationsManager.DecryptField(integrationData.Data.EncryptedFields["authtoken"]),
                        callbackUrl,
                        _serviceProvider.GetRequiredService<TwilioManager>(),
                        _serviceProvider.GetRequiredService<ILogger<TwilioConversationClient>>());
                    return result;

                default:
                    result.Message = $"Unsupported provider {clientData.Provider}";
                    return result;
            }
        }

        private async Task<IConversationAgent> CreateAIAgentAsync(string sessionId, ConversationSessionConfiguration config, ConversationSessionManager sessionManager)
        {
            // Create agent ID
            string agentId = $"ai_{sessionId}";

            try
            {
                // Create the AI agent
                return new ConversationAIAgent(
                    _serviceProvider.GetRequiredService<ILoggerFactory>(),
                    sessionManager,
                    agentId,
                    _businessManager,
                    _serviceProvider.GetRequiredService<SystemPromptGenerator>(),
                    _serviceProvider.GetRequiredService<STTProviderManager>(),
                    _serviceProvider.GetRequiredService<TTSProviderManager>(),
                    _serviceProvider.GetRequiredService<LLMProviderManager>()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating AI agent");
                throw;
            }
        }

        private async Task<OutboundCallResultModel> InitiateModemTelOutboundCallAsync(
            long businessId,
            BusinessNumberModemTelData phoneNumber,
            BusinessAppIntegration integration,
            string toNumber,
            string queueId)
        {
            var result = new OutboundCallResultModel();

            try
            {
                // Get API credentials
                string apiKey = _integrationsManager.DecryptField(integration.EncryptedFields["apikey"]);
                string apiBaseUrl = integration.Fields["endpoint"];

                // Use the ModemTel manager to initiate the call
                var modemTelManager = _serviceProvider.GetRequiredService<ModemTelManager>();

                // Initiate the call
                var callResult = await modemTelManager.MakeCallAsync(
                    apiKey,
                    apiBaseUrl,
                    phoneNumber.ModemTelPhoneNumberId,
                    toNumber);

                if (!callResult.Success || callResult.Data == null)
                {
                    result.Message = $"Failed to initiate ModemTel call: {callResult.Message}";
                    return result;
                }

                // Set successful result
                result.Success = true;
                result.CallId = callResult.Data.Id;
                result.Status = callResult.Data.Status;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating ModemTel outbound call");
                result.Message = $"Error initiating call: {ex.Message}";
                return result;
            }
        }

        private async Task<OutboundCallResultModel> InitiateTwilioOutboundCallAsync(long businessId, BusinessNumberTwilioData phoneNumber, BusinessAppIntegration integration, string toNumber, string queueId)
        {
            var result = new OutboundCallResultModel();

            try
            {
                // Get API credentials
                string accountSid = integration.Fields["accountsid"];
                string authToken = _integrationsManager.DecryptField(integration.EncryptedFields["authToken"]);

                // Use the Twilio manager to initiate the call
                var twilioManager = _serviceProvider.GetRequiredService<TwilioManager>();

                // Create callback URL for this server
                var callbackUrl = $"TODO/api/call/twilio-callback/{queueId}";

                // Initiate the call
                var callResult = await twilioManager.MakeCallAsync(
                    accountSid,
                    authToken,
                    phoneNumber.Number,
                    toNumber,
                    callbackUrl);

                if (!callResult.Success || callResult.Data == null)
                {
                    result.Message = $"Failed to initiate Twilio call: {callResult.Message}";
                    return result;
                }

                // Set successful result
                result.Success = true;
                result.CallId = callResult.Data.Sid;
                result.Status = callResult.Data.Status;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Twilio outbound call");
                result.Message = $"Error initiating call: {ex.Message}";
                return result;
            }
        }
    }
}