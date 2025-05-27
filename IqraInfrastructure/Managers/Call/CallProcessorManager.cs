using Deepgram.Models.Agent.v2.WebSocket;
using IqraCore.Entities.Business;
using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Conversation;
using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Region;
using IqraCore.Entities.Server;
using IqraCore.Interfaces.Conversation;
using IqraCore.Models.Server;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Call.Helper;
using IqraInfrastructure.Managers.Conversation;
using IqraInfrastructure.Managers.Conversation.Agent.AI;
using IqraInfrastructure.Managers.Conversation.Agent.AI.Helpers;
using IqraInfrastructure.Managers.Conversation.Client;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.Languages;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Managers.Server.Metrics;
using IqraInfrastructure.Managers.STT;
using IqraInfrastructure.Managers.Telephony;
using IqraInfrastructure.Managers.TTS;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.Call;
using IqraInfrastructure.Repositories.Conversation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace IqraInfrastructure.Managers.Call
{
    public class CallProcessorManager
    {
        private BackendAppConfig _backendAppConfig;

        private readonly ILogger<CallProcessorManager> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ServerMetricsMonitor _serverMetricsMonitor;
        private readonly InboundCallQueueRepository _inboundCallQueueRepository;
        private readonly OutboundCallQueueRepository _outboundCallQueueRepository;
        private readonly ConversationStateRepository _conversationStateRepository;
        private readonly BusinessManager _businessManager;
        private readonly IntegrationsManager _integrationsManager;
        private readonly RegionManager _regionManager;

        // combine the two
        private readonly ConcurrentDictionary<string, ConversationSession> _activeSessions = new();
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _ctsSessions = new();

        private readonly SemaphoreSlim _sessionCreationLock = new SemaphoreSlim(1, 1);

        private readonly CancellationTokenSource _processorCTS = new CancellationTokenSource();

        public CallProcessorManager(
            ILogger<CallProcessorManager> logger,
            IServiceProvider serviceProvider,
            BackendAppConfig backendAppConfig,
            ServerMetricsMonitor serverMetricsMonitor,
            InboundCallQueueRepository inboundCallQueueRepository,
            OutboundCallQueueRepository outboundCallQueueRepository,
            ConversationStateRepository conversationStateRepository,
            BusinessManager businessManager,
            IntegrationsManager integrationsManager,
            RegionManager regionManager
        )
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _backendAppConfig = backendAppConfig;
            _serverMetricsMonitor = serverMetricsMonitor;
            _inboundCallQueueRepository = inboundCallQueueRepository;
            _outboundCallQueueRepository = outboundCallQueueRepository;
            _conversationStateRepository = conversationStateRepository;
            _businessManager = businessManager;
            _integrationsManager = integrationsManager;
            _regionManager = regionManager;
        }

        public async Task<FunctionReturnResult<ProcessedInboundCallResponse?>> ProcessInboundCallAsync(string queueId)
        {
            var result = new FunctionReturnResult<ProcessedInboundCallResponse?>();

            FunctionReturnResult<ConversationSession?>? sessionResult = null;

            var sessionBitPerSample = 16;
            var sessionChannels = 1;
            var sessionSampleRate = 8000;

            try
            {
                InboundCallQueueData? inboundQueueData = await _inboundCallQueueRepository.GetInboundCallQueueByIdAsync(queueId);
                if (inboundQueueData == null)
                {
                    return result.SetFailureResult("ProcessInboundCallAsync:QUEUE_NOT_FOUND", "Queue not found");
                }
                await _inboundCallQueueRepository.UpdateInboundCallQueueStatusAsync(queueId, CallQueueStatusEnum.ProcessingBackend);

                RegionData? currentRegionData = await _regionManager.GetRegionById(_backendAppConfig.RegionId);
                if (currentRegionData == null)
                {
                    return result.SetFailureResult("ProcessInboundCallAsync:REGION_NOT_FOUND", "Region not found");
                }
                RegionServerData? regionServerData = currentRegionData.Servers.First(x => x.Endpoint == _backendAppConfig.ServerId);
                if (regionServerData == null)
                {
                    return result.SetFailureResult("ProcessInboundCallAsync:REGION_SERVER_NOT_FOUND", "Region server not found");
                }

                sessionResult = await CreateConversationSessionAsync(inboundQueueData);
                if (!sessionResult.Success)
                {
                    return result.SetFailureResult("ProcessInboundCallAsync:SESSION_CREATION_FAILED", sessionResult.Message);
                }
                var startSessionResult = await sessionResult.Data.InitalizeAsync();
                if (!startSessionResult.Success)
                {
                    return result.SetFailureResult("ProcessInboundCallAsync:SESSION_INIT_FAILED", startSessionResult.Message);
                }

                var taskResultSuccess = false;
                IConversationAgent? primaryAIAgent = null;
                bool hasAddedAgent = false;
                IConversationClient? primaryTelephonyClient = null;
                bool hasAddedClient = false;
                await Task.Run(async () =>
                {
                    try
                    {
                        var sessionAIAgentResult = await CreateAIAgentAsync(sessionResult.Data, new ConversationAgentConfiguration() { BitsPerSample = sessionBitPerSample, Channels = sessionChannels, SampleRate = sessionSampleRate });
                        if (!sessionAIAgentResult.Success)
                        {
                            await _conversationStateRepository.AddLogEntryAsync(sessionResult.Data.SessionId, new ConversationLogEntry() { Timestamp = DateTime.UtcNow, Message = $"[ProcessInboundCallAsync:{sessionAIAgentResult.Code}] {sessionAIAgentResult.Message}" });
                            return;
                        }
                        primaryAIAgent = sessionAIAgentResult.Data;

                        var sessionTelephonyResult = await CreateTelephonyClient(inboundQueueData, sessionResult.Data);
                        if (!sessionTelephonyResult.Success)
                        {
                            await _conversationStateRepository.AddLogEntryAsync(sessionResult.Data.SessionId, new ConversationLogEntry() { Timestamp = DateTime.UtcNow, Message = $"[ProcessInboundCallAsync:{sessionTelephonyResult.Code}] {sessionTelephonyResult.Message}" });
                            return;
                        }
                        primaryTelephonyClient = sessionTelephonyResult.Data;

                        var addSessionAgentResult = await sessionResult.Data.AddPrimaryAgent(primaryAIAgent);
                        if (!addSessionAgentResult.Success)
                        {
                            await _conversationStateRepository.AddLogEntryAsync(sessionResult.Data.SessionId, new ConversationLogEntry() { Timestamp = DateTime.UtcNow, Message = $"[ProcessInboundCallAsync:{addSessionAgentResult.Code}] {addSessionAgentResult.Message}" });
                            return;
                        }
                        hasAddedAgent = true;

                        var addSessionTelephonyResult = await sessionResult.Data.AddPrimaryClient(primaryTelephonyClient, new ConversationClientConfiguration() { QueueData = inboundQueueData, BitsPerSample = sessionBitPerSample, Channels = sessionChannels, SampleRate = sessionSampleRate });
                        if (!addSessionTelephonyResult.Success)
                        {
                            await _conversationStateRepository.AddLogEntryAsync(sessionResult.Data.SessionId, new ConversationLogEntry() { Timestamp = DateTime.UtcNow, Message = $"[ProcessInboundCallAsync:{addSessionTelephonyResult.Code}] {addSessionTelephonyResult.Message}" });
                            return;
                        }
                        hasAddedClient = true;

                        taskResultSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        await _conversationStateRepository.AddLogEntryAsync(sessionResult.Data.SessionId, new ConversationLogEntry() { Timestamp = DateTime.UtcNow, Message = $"[ProcessInboundCallAsync:EXECEPTION] {ex.Message}" });
                    }
                    finally
                    {
                        if (!taskResultSuccess)
                        {
                            if (hasAddedAgent == false)
                            {
                                if (primaryAIAgent != null)
                                {
                                    primaryAIAgent.Dispose();
                                }
                            }

                            if (hasAddedClient == false)
                            {
                                if (primaryTelephonyClient != null)
                                {
                                    primaryTelephonyClient.Dispose();
                                }
                            }

                            await sessionResult.Data.EndAsync("ProcessInboundCall Failed", ConversationSessionState.Error);
                            await CleanupSessionAsync(sessionResult.Data.SessionId);
                        }
                    }
                });

                if (!taskResultSuccess)
                {
                    return result.SetFailureResult("ProcessInboundCallAsync:SESSION_CONNECTORS_INIT_FAILED", "Session (agent/telephony) init failed");
                }

                var generatedWebhookToken = CallWebsocketTokenGenerator.GenerateHmacToken(sessionResult.Data.SessionId, primaryTelephonyClient.ClientId, TimeSpan.FromMinutes(5), _backendAppConfig.WebhookTokenSecret);
                var webhookUrl = BuildWebhookUrl(regionServerData, sessionResult.Data.SessionId, primaryTelephonyClient.ClientId, generatedWebhookToken);

                return result.SetSuccessResult(
                    new ProcessedInboundCallResponse()
                    {
                        SessionId = sessionResult.Data.SessionId,
                        WebhookUrl = webhookUrl.ToString()
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating outbound call");
                result.Message = $"Error initiating call: {ex.Message}";
                return result;
            }
            finally
            {
                if (!result.Success)
                {
                    if (sessionResult != null && sessionResult.Data != null)
                    {
                        await sessionResult.Data.EndAsync("ProcessInboundCall Failed", ConversationSessionState.Error);
                        await CleanupSessionAsync(sessionResult.Data.SessionId);
                    }
                }
            }
        }

        public async Task<FunctionReturnResult> InitiateOutboundCallAsync(string queueId)
        {
            var result = new FunctionReturnResult();

            FunctionReturnResult<ConversationSession?>? sessionResult = null;

            var sessionBitPerSample = 16;
            var sessionChannels = 1;
            var sessionSampleRate = 8000;

            try
            {
                OutboundCallQueueData? outboundQueueData = await _outboundCallQueueRepository.GetOutboundCallQueueByIdAsync(queueId);
                if (outboundQueueData == null)
                {
                    return result.SetFailureResult("InitiateOutboundCallAsync:QUEUE_NOT_FOUND", "Queue not found");
                }
                await _outboundCallQueueRepository.UpdateCallStatusAsync(queueId, CallQueueStatusEnum.ProcessingBackend);

                var businessNumber = await _businessManager.GetNumberManager().GetBusinessNumberById(outboundQueueData.BusinessId, outboundQueueData.CallingNumberId);
                if (businessNumber == null)
                {
                    return result.SetFailureResult("InitiateOutboundCallAsync:BUSINESS_NUMBER_NOT_FOUND", "Business number not found");
                }

                var integrationResult = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(outboundQueueData.BusinessId, businessNumber.IntegrationId);
                if (!integrationResult.Success || integrationResult.Data == null)
                {
                    return result.SetFailureResult($"InitiateOutboundCallAsync:{integrationResult.Code}", integrationResult.Message);
                }

                var regionData = await _regionManager.GetRegionById(_backendAppConfig.RegionId);
                if (regionData == null)
                {
                    return result.SetFailureResult("InitiateOutboundCallAsync:REGION_NOT_FOUND", "Region not found");
                }
                var regionServerData = regionData.Servers.FirstOrDefault(s => s.Endpoint == _backendAppConfig.ServerId);
                if (regionServerData == null)
                {
                    return result.SetFailureResult("InitiateOutboundCallAsync:REGION_SERVER_NOT_FOUND", "Region server not found");
                }

                sessionResult = await CreateConversationSessionAsync(outboundQueueData);
                if (!sessionResult.Success)
                {
                    return result.SetFailureResult("InitiateOutboundCallAsync:SESSION_CREATION_FAILED", sessionResult.Message);
                }
                var startSessionResult = await sessionResult.Data.InitalizeAsync();
                if (!startSessionResult.Success)
                {
                    return result.SetFailureResult("InitiateOutboundCallAsync:SESSION_INIT_FAILED", startSessionResult.Message);
                }

                var taskResultSuccess = false;
                IConversationAgent? agent = null;
                bool hasAddedAgent = false;
                IConversationClient? primaryTelephonyClient = null;
                bool hasAddedClient = false;
                await Task.Run(async () =>
                {
                    try
                    {
                        var sessionAIAgentResult = await CreateAIAgentAsync(sessionResult.Data, new ConversationAgentConfiguration() { BitsPerSample = sessionBitPerSample, Channels = sessionChannels, SampleRate = sessionSampleRate });
                        if (!sessionAIAgentResult.Success)
                        {
                            await _conversationStateRepository.AddLogEntryAsync(sessionResult.Data.SessionId, new ConversationLogEntry() { Timestamp = DateTime.UtcNow, Message = $"[InitiateOutboundCallAsync:{sessionAIAgentResult.Code}] {sessionAIAgentResult.Message}" });
                            return;
                        }
                        agent = sessionAIAgentResult.Data;

                        var sessionTelephonyResult = await CreateTelephonyClient(outboundQueueData, sessionResult.Data);
                        if (!sessionTelephonyResult.Success)
                        {
                            await _conversationStateRepository.AddLogEntryAsync(sessionResult.Data.SessionId, new ConversationLogEntry() { Timestamp = DateTime.UtcNow, Message = $"[InitiateOutboundCallAsync:{sessionTelephonyResult.Code}] {sessionTelephonyResult.Message}" });
                            return;
                        }
                        primaryTelephonyClient = sessionTelephonyResult.Data;

                        var addSessionAgentResult = await sessionResult.Data.AddPrimaryAgent(agent);
                        if (!addSessionAgentResult.Success)
                        {
                            await _conversationStateRepository.AddLogEntryAsync(sessionResult.Data.SessionId, new ConversationLogEntry() { Timestamp = DateTime.UtcNow, Message = $"[InitiateOutboundCallAsync:{addSessionAgentResult.Code}] {addSessionAgentResult.Message}" });
                            return;
                        }
                        hasAddedAgent = true;

                        var addSessionTelephonyResult = await sessionResult.Data.AddPrimaryClient(primaryTelephonyClient, new ConversationClientConfiguration() { QueueData = outboundQueueData, BitsPerSample = sessionBitPerSample, Channels = sessionChannels, SampleRate = sessionSampleRate });
                        if (!addSessionTelephonyResult.Success)
                        {
                            await _conversationStateRepository.AddLogEntryAsync(sessionResult.Data.SessionId, new ConversationLogEntry() { Timestamp = DateTime.UtcNow, Message = $"[InitiateOutboundCallAsync:{addSessionTelephonyResult.Code}] {addSessionTelephonyResult.Message}" });
                            return;
                        }
                        hasAddedClient = true;

                        taskResultSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        await _conversationStateRepository.AddLogEntryAsync(sessionResult.Data.SessionId, new ConversationLogEntry() { Timestamp = DateTime.UtcNow, Message = $"[InitiateOutboundCallAsync:EXECEPTION] {ex.Message}" });
                    }
                    finally
                    {
                        if (!taskResultSuccess)
                        {
                            if (hasAddedAgent == false)
                            {
                                if (agent != null)
                                {
                                    agent.Dispose();
                                }
                            }

                            if (hasAddedClient == false)
                            {
                                if (primaryTelephonyClient != null)
                                {
                                    primaryTelephonyClient.Dispose();
                                }
                            }

                            await sessionResult.Data.EndAsync("ProcessInboundCall Failed", ConversationSessionState.Error);
                            await CleanupSessionAsync(sessionResult.Data.SessionId);
                        }
                    }
                });

                var generatedWebhookToken = CallWebsocketTokenGenerator.GenerateHmacToken(sessionResult.Data.SessionId, primaryTelephonyClient.ClientId, TimeSpan.FromMinutes(5), _backendAppConfig.WebhookTokenSecret);
                var webhookUrl = BuildWebhookUrl(regionServerData, sessionResult.Data.SessionId, primaryTelephonyClient.ClientId, generatedWebhookToken);

                OutboundCallResultModel? callResultModel = null;
                switch (businessNumber.Provider)
                {
                    case TelephonyProviderEnum.ModemTel:
                        callResultModel = await InitiateModemTelOutboundCallAsync(
                            businessNumber as BusinessNumberModemTelData,
                            integrationResult.Data,
                            outboundQueueData.RecipientNumber,
                            sessionResult.Data.SessionId,
                            webhookUrl,
                            regionServerData
                        );
                        break;

                    case TelephonyProviderEnum.Twilio:
                        callResultModel = await InitiateTwilioOutboundCallAsync(
                            businessNumber as BusinessNumberTwilioData,
                            integrationResult.Data,
                            outboundQueueData.RecipientNumber,
                            queueId,
                            webhookUrl,
                            regionServerData
                        );
                        break;

                    default:
                        return result.SetFailureResult("InitiateOutboundCallAsync:INVALID_PROVIDER", "Invalid number provider");
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("InitiateOutboundCallAsync:EXCEPTION", ex.Message);
            }
            finally
            {
                if (!result.Success)
                {
                    if (sessionResult != null && sessionResult.Data != null)
                    {
                        await sessionResult.Data.EndAsync("InitiateOutboundCall Failed", ConversationSessionState.Error);
                        await CleanupSessionAsync(sessionResult.Data.SessionId);
                    }
                }
            }
        }
        public async Task<FunctionReturnResult> NotifyTelephonyClientStatus(string sessionId, TelephonyStatusNotifyToBackendModel request)
        {
            var result = new FunctionReturnResult();

            try
            {
                if (!_activeSessions.TryGetValue(sessionId, out var sessionData))
                {
                    return result.SetFailureResult("NotifyTelephonyClientStatus:SESSION_NOT_FOUND", "Session not found");
                }

                switch (request.Status)
                {
                    case "in-progress":
                        {
                            if (sessionData.State != ConversationSessionState.WaitingForPrimaryClient)
                            {
                                return result.SetFailureResult("NotifyTelephonyClientStatus:INVALID_STATE", "Invalid session state");
                            }

                            await sessionData.StartAsync();
                            
                            return result.SetSuccessResult();
                        }

                    case "completed":
                        {
                            if (sessionData.State == ConversationSessionState.Ending || sessionData.State == ConversationSessionState.Ended)
                            {
                                // already ending or ended no need to do anything
                                return result.SetSuccessResult();
                            }

                            BaseTelephonyConversationClient? telephonyClient = (BaseTelephonyConversationClient)sessionData.GetTelephonyClientByProviderPhoneNumberId(request.Provider, request.PhoneNumberId);
                            if (telephonyClient == null)
                            {
                                return result.SetFailureResult("NotifyTelephonyClientStatus:CLIENT_NOT_FOUND", "Client not found");
                            }

                            await telephonyClient.DisconnectAsync("Recieved completed status by telephony provider");
                            
                            return result.SetSuccessResult();
                        }

                    case "busy":
                        {
                            // for outbound calls, we need to end the session and clean it up, check for retry logic and requeue if needed
                            goto default;
                        }

                    default:
                        return result.SetFailureResult("NotifyTelephonyClientStatus:UNHANDLED_STATUS", "unhandled status type");
                }
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("NotifyTelephonyClientStatus:EXCEPTION", ex.Message);
            }
        }
        private async Task CleanupSessionAsync(string sessionId)
        {
            // Remove from active sessions
            _activeSessions.TryRemove(sessionId, out _);

            // Cancel and dispose token
            if (_ctsSessions.TryRemove(sessionId, out var cts))
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
        
        private async Task<FunctionReturnResult<ConversationSession?>> CreateConversationSessionAsync(CallQueueData queueData)
        {
            var result = new FunctionReturnResult<ConversationSession?>();

            if (!_serverMetricsMonitor.HasCapacity())
            {
                return result.SetFailureResult("CreateConversationSessionAsync:NO_SERVER_CAPACITY", "No capacity available on server");
            }

            // todo create ids better using database count system
            string sessionId = Guid.NewGuid().ToString();
            try
            {
                await _sessionCreationLock.WaitAsync(_processorCTS.Token);    
                CancellationTokenSource newSessionCTS = new CancellationTokenSource();
                CancellationTokenSource combinedCTS = CancellationTokenSource.CreateLinkedTokenSource(newSessionCTS.Token, _processorCTS.Token);

                var conversationSession = new ConversationSession(
                    sessionId,
                    queueData,
                    "call",
                    combinedCTS,

                    _businessManager,
                    _inboundCallQueueRepository,
                    _conversationStateRepository,
                    _serviceProvider.GetRequiredService<ConversationAudioRepository>(),
                    _serviceProvider.GetRequiredService<ILoggerFactory>()                  
                );

                _activeSessions[sessionId] = conversationSession;
                _ctsSessions[sessionId] = newSessionCTS;

                return result.SetSuccessResult(conversationSession);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("CreateConversationSessionAsync:ERROR_CREATING_SESSION", ex.Message);
            }
            finally
            {
                _sessionCreationLock.Release();
            }
        }
        private async Task<FunctionReturnResult<IConversationClient?>> CreateTelephonyClient(CallQueueData queueData, ConversationSession sessionManager)
        {
            var result = new FunctionReturnResult<IConversationClient?>();

            // Create a client ID from session ID and provider
            string clientId;
            string numberId;
            string? callId;
            if (queueData.Type == CallQueueTypeEnum.Inbound)
            {
                var inboundCallQueueData = queueData as InboundCallQueueData;
                clientId = $"client_{inboundCallQueueData.RouteNumberId}";
                numberId = inboundCallQueueData.RouteNumberId;
                callId = inboundCallQueueData.ProviderCallId;
            }
            else if (queueData.Type == CallQueueTypeEnum.Outbound)
            {
                var outboundCallQeueData = queueData as OutboundCallQueueData;
                clientId = $"client_{outboundCallQeueData.CallingNumberId}";
                numberId = outboundCallQeueData.CallingNumberId;
                callId = null;
            }
            else
            {
                return result.SetFailureResult("CreateTelephonyClient:INVALID_QUEUE_TYPE", "Invalid queue type");
            }

            var businessNumberData = await _businessManager.GetNumberManager().GetBusinessNumberById(queueData.BusinessId, numberId);
            if (businessNumberData == null)
            {
                _logger.LogError("Business number not found for business {BusinessId}", queueData.BusinessId);

                result.Message = "Business number not found";
                return result;
            }
            var integrationData = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(queueData.BusinessId, businessNumberData.IntegrationId);
            if (!integrationData.Success || integrationData.Data == null)
            {
                result.Message = "Integration not found";
                _logger.LogError("Integration not found for business {BusinessId}", queueData.BusinessId);
                return result;
            }

            string phoneNumberData = businessNumberData.CountryCode + businessNumberData.Number; // todo this is alphabet country code not number

            switch (businessNumberData.Provider)
            {
                case TelephonyProviderEnum.ModemTel:
                    return result.SetSuccessResult(
                        new ModemTelConversationClient(
                            clientId,
                            phoneNumberData,
                            callId,
                            integrationData.Data.Fields["endpoint"],
                            _integrationsManager.DecryptField(integrationData.Data.EncryptedFields["apikey"]),
                            _serviceProvider.GetRequiredService<ModemTelManager>(),
                            _serviceProvider.GetRequiredService<ILogger<ModemTelConversationClient>>()
                        )
                    );

                case TelephonyProviderEnum.Twilio:
                    return result.SetSuccessResult(
                        new TwilioConversationClient(
                            clientId,
                            phoneNumberData,
                            callId,
                            integrationData.Data.Fields["accountsid"],
                            _integrationsManager.DecryptField(integrationData.Data.EncryptedFields["authtoken"]),
                            _serviceProvider.GetRequiredService<TwilioManager>(),
                            _serviceProvider.GetRequiredService<ILogger<TwilioConversationClient>>()
                        )
                    );

                default:
                    return result.SetFailureResult("CreateTelephonyClient:INVALID_PROVIDER", "Invalid provider");
            }
        }
        private async Task<FunctionReturnResult<IConversationAgent?>> CreateAIAgentAsync(ConversationSession sessionManager, ConversationAgentConfiguration agentConfiguration)
        {
            var result = new FunctionReturnResult<IConversationAgent?>();

            // Create agent ID
            string agentId = $"ai_{sessionManager.SessionId}";
            try
            {
                var AIAgent = new ConversationAIAgent(
                    _serviceProvider.GetRequiredService<ILoggerFactory>(),
                    sessionManager,
                    agentId,
                    agentConfiguration,
                    _businessManager,
                    _serviceProvider.GetRequiredService<SystemPromptGenerator>(),
                    _serviceProvider.GetRequiredService<STTProviderManager>(),
                    _serviceProvider.GetRequiredService<TTSProviderManager>(),
                    _serviceProvider.GetRequiredService<LLMProviderManager>(),
                    _serviceProvider.GetRequiredService<LanguagesManager>(),
                    _serviceProvider.GetRequiredService<BusinessAgentAudioRepository>()
                );

                return result.SetSuccessResult(AIAgent);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("CreateAIAgentAsync:EXCEPTION", $"Failed to create AI agent: {ex.Message}");
            }
        }

        private async Task<OutboundCallResultModel> InitiateModemTelOutboundCallAsync(BusinessNumberModemTelData phoneNumber, BusinessAppIntegration integration, string toNumber, string sessionId, string websocketUrl, RegionServerData regionServer)
        {
            var result = new OutboundCallResultModel();
            try
            {
                string apiKey = _integrationsManager.DecryptField(integration.EncryptedFields["apikey"]);
                string apiBaseUrl = integration.Fields["endpoint"];
                var modemTelManager = _serviceProvider.GetRequiredService<ModemTelManager>();

                var statusCallbackUrl = new Uri((regionServer.UseSSL ? "https://" : "http://") + regionServer.Endpoint);
                statusCallbackUrl = new Uri(statusCallbackUrl, $"api/call/status/modemtel/status/{sessionId}");

                var callResult = await modemTelManager.MakeCallAsync(
                    apiKey,
                    apiBaseUrl,
                    phoneNumber.ModemTelPhoneNumberId,
                    toNumber,
                    statusCallbackUrl.ToString(),
                    websocketUrl
                );

                if (!callResult.Success || callResult.Data == null)
                {
                    result.Message = $"Failed to initiate ModemTel call: {callResult.Message}"; return result;
                }
                result.Success = true; result.CallId = callResult.Data.Id; result.Status = callResult.Data.Status;
                return result;
            }
            catch (Exception ex)
            {
                result.Message = $"Error initiating call: {ex.Message}"; return result;
            }
        }
        private async Task<OutboundCallResultModel> InitiateTwilioOutboundCallAsync(BusinessNumberTwilioData phoneNumber, BusinessAppIntegration integration, string toNumber, string sessionId, string websocketUrl, RegionServerData regionServer)
        {
            var result = new OutboundCallResultModel();
            try
            {
                string accountSid = integration.Fields["accountsid"];
                string authToken = _integrationsManager.DecryptField(integration.EncryptedFields["authToken"]);
                var twilioManager = _serviceProvider.GetRequiredService<TwilioManager>();

                var statusCallbackUrl = new Uri((regionServer.UseSSL ? "https://" : "http://") + regionServer.Endpoint);
                statusCallbackUrl = new Uri(statusCallbackUrl, $"api/call/status/twilio/status/{sessionId}");

                var callResult = await twilioManager.MakeCallAsync(
                    accountSid,
                    authToken,
                    phoneNumber.Number,
                    toNumber,
                    statusCallbackUrl.ToString(), // Status callback for Twilio
                    websocketUrl
                );

                if (!callResult.Success || callResult.Data == null)
                {
                    result.Message = $"Failed to initiate Twilio call: {callResult.Message}"; return result;
                }
                result.Success = true; result.CallId = callResult.Data.Sid; result.Status = callResult.Data.Status;
                return result;
            }
            catch (Exception ex)
            {
                result.Message = $"Error initiating call: {ex.Message}"; return result;
            }
        }

        public async Task<FunctionReturnResult> AssignWebSocketToClientAsync(string sessionId, string clientId, string sessionToken, WebSocket webSocket)
        {
            var result = new FunctionReturnResult();

            var validatedSessionTokenResult = CallWebsocketTokenGenerator.ValidateHmacToken(sessionToken, sessionId, clientId, _backendAppConfig.WebhookTokenSecret, out var validationError);
            if (!validatedSessionTokenResult)
            {
                return result.SetFailureResult("AssignWebSocketToClientAsync:VALIDATION_FAILED", validationError);
            }

            if (_activeSessions.TryGetValue(sessionId, out var sessionManager) && _ctsSessions.TryGetValue(sessionId, out var sessionOverallCts))
            {
                IConversationClient? convClient = null;
                if (sessionManager.PrimaryClient?.ClientId == clientId)
                {
                    convClient = sessionManager.PrimaryClient;
                }

                if (convClient is WebSocketCapableConversationClient wsClient)
                {
                    await wsClient.HandleAcceptedWebSocketAsync(webSocket, sessionOverallCts.Token);
                    return result.SetSuccessResult();
                }
                else
                {
                    return result.SetFailureResult("AssignWebSocketToClientAsync:NOT_FOUND", "Client not websocket capable or not found");
                }
            }
            else
            {
                return result.SetFailureResult("AssignWebSocketToClientAsync:SESSION_NOT_FOUND", "Session or session cts not found");
            }
        }

        private string BuildWebhookUrl(RegionServerData serverData, string sessionId, string clientId, string sessionToken)
        {
            return new Uri(new Uri((serverData.UseSSL ? "wss://" : "ws://") + serverData.Endpoint), $"/ws/session/{sessionId}/client/{clientId}/{sessionToken}").ToString();
        }
    }
}