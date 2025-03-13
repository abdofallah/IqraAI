using IqraCore.Entities.Business;
using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Server;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.Conversation;
using IqraCore.Models.Server;
using IqraCore.Models.Telephony;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Conversation;
using IqraInfrastructure.Managers.Conversation.Client;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.Script;
using IqraInfrastructure.Managers.Server;
using IqraInfrastructure.Managers.Telephony;
using IqraInfrastructure.Repositories.Conversation;
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
        private readonly ConversationStateRepository _conversationStateRepository;
        private readonly BusinessManager _businessManager;
        private readonly IntegrationsManager _integrationsManager;

        private readonly ConcurrentDictionary<string, IConversationSession> _activeSessions = new();
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _sessionCancellationTokens = new();
        private readonly SemaphoreSlim _sessionCreationLock = new SemaphoreSlim(1, 1);

        public CallProcessorManager(
            ILogger<CallProcessorManager> logger,
            IServiceProvider serviceProvider,
            ServerStatusManager serverStatusService,
            ConversationStateRepository conversationStateRepository,
            BusinessManager businessManager,
            IntegrationsManager integrationsManager)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _serverStatusManager = serverStatusService;
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
                    config,
                    _conversationStateRepository,
                    _serviceProvider.GetRequiredService<ConversationAudioRepository>(),
                    _serviceProvider.GetRequiredService<ILogger<ConversationSessionManager>>());

                // Create telephony client based on provider
                IConversationClient telephonyClient = CreateTelephonyClient(clientData, sessionId);

                // Add client to session
                await conversationSession.AddClientAsync(telephonyClient);

                // Create and add AI agent
                var agent = await CreateAIAgentAsync(sessionId, config);
                if (agent != null)
                {
                    await conversationSession.AddAgentAsync(agent, new ConversationAgentConfiguration
                    {
                        BusinessId = config.BusinessId,
                        BusinessAgentId = await GetBusinessAgentIdFromRouteAsync(config.BusinessId, config.RouteId),
                        RouteId = config.RouteId
                    });
                }

                // Start the session
                await conversationSession.StartAsync(sessionCts.Token);

                // Store in active sessions
                _activeSessions[sessionId] = conversationSession;

                // Update server status
                _serverStatusManager.IncrementActiveCalls();

                _logger.LogInformation("Created conversation session {SessionId} for business {BusinessId}",
                    sessionId, config.BusinessId);

                return sessionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating conversation session");
                await CleanupSessionAsync(sessionId);
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

        private IConversationClient CreateTelephonyClient(TelephonyWebhookContextModel clientData, string sessionId)
        {
            // Create a client ID from session ID and provider
            string clientId = $"{clientData.Provider}_{sessionId}";

            switch (clientData.Provider)
            {
                case TelephonyProviderEnum.ModemTel:
                    // Get required ModemTel data
                    var token = clientData.AdditionalData["mediaSessionToken"];
                    var wsUrl = clientData.AdditionalData["mediaSessionWebSocketUrl"];

                    return new ModemTelConversationClient(
                        clientId,
                        clientData.CallId,
                        GetApiKeyForBusiness(clientData.BusinessId, clientData.Provider),
                        GetApiEndpointForBusiness(clientData.BusinessId, clientData.Provider),
                        token,
                        wsUrl,
                        _serviceProvider.GetRequiredService<ModemTelManager>(),
                        _serviceProvider.GetRequiredService<ILogger<ModemTelConversationClient>>());

                case TelephonyProviderEnum.Twilio:
                    // Get required Twilio data
                    var accountSid = clientData.AdditionalData["accountSid"];
                    var callbackUrl = clientData.AdditionalData["callbackUrl"];

                    return new TwilioConversationClient(
                        clientId,
                        clientData.CallId,
                        accountSid,
                        GetAuthTokenForBusiness(clientData.BusinessId, clientData.Provider),
                        callbackUrl,
                        _serviceProvider.GetRequiredService<TwilioManager>(),
                        _serviceProvider.GetRequiredService<ILogger<TwilioConversationClient>>());

                default:
                    throw new NotSupportedException($"Unsupported telephony provider: {clientData.Provider}");
            }
        }

        private async Task<IConversationAgent> CreateAIAgentAsync(string sessionId, ConversationSessionConfiguration config)
        {
            // Create agent ID
            string agentId = $"ai_{sessionId}";

            try
            {
                // Create the AI agent
                return new AIAgent(
                    agentId,
                    _serviceProvider.GetRequiredService<ISTTService>(),
                    _serviceProvider.GetRequiredService<ITTSService>(),
                    _serviceProvider.GetRequiredService<ILLMService>(),
                    _businessManager,
                    _serviceProvider.GetRequiredService<SystemPromptGenerator>(),
                    _serviceProvider.GetRequiredService<ScriptExecutionManager>(),
                    _serviceProvider.GetRequiredService<ILogger<AIAgent>>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating AI agent");
                throw;
            }
        }

        private async Task<string> GetBusinessAgentIdFromRouteAsync(long businessId, string routeId)
        {
            try
            {
                var businessApp = await _businessManager.GetUserBusinessAppById(businessId, "GetBusinessAgentIdFromRouteAsync");
                if (!businessApp.Success || businessApp.Data == null)
                {
                    throw new InvalidOperationException($"Business app not found: {businessId}");
                }

                var route = businessApp.Data.Routings.FirstOrDefault(r => r.Id == routeId);
                if (route == null)
                {
                    throw new InvalidOperationException($"Route not found: {routeId}");
                }

                return route.Agent.SelectedAgentId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting agent ID from route");
                throw;
            }
        }

        private string GetApiKeyForBusiness(long businessId, TelephonyProviderEnum provider)
        {
            // Implementation depends on where you store API keys
            // This would typically retrieve the decrypted API key from your integration data
            return "placeholder_api_key";
        }

        private string GetApiEndpointForBusiness(long businessId, TelephonyProviderEnum provider)
        {
            // Implementation depends on where you store API endpoints
            return "https://api.example.com";
        }

        private string GetAuthTokenForBusiness(long businessId, TelephonyProviderEnum provider)
        {
            // Implementation to retrieve auth token
            return "placeholder_auth_token";
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

                // Create callback URL for this server
                var callbackUrl = $"{_serviceProvider.GetRequiredService<ServerConfig>().PublicBaseUrl}/api/call/modemtel-callback/{queueId}";

                // Initiate the call
                var callResult = await modemTelManager.MakeCallAsync(
                    apiKey,
                    apiBaseUrl,
                    phoneNumber.ModemTelPhoneNumberId,
                    toNumber,
                    callbackUrl);

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
                var callbackUrl = $"{_serviceProvider.GetRequiredService<ServerConfig>().PublicBaseUrl}/api/call/twilio-callback/{queueId}";

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