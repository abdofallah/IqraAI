using IqraCore.Entities.Business;
using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Conversation;
using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Helper.Server;
using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Region;
using IqraCore.Entities.Server;
using IqraCore.Interfaces.Conversation;
using IqraCore.Models.Server;
using IqraInfrastructure.Managers.Billing;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Call.Helper;
using IqraInfrastructure.Managers.Conversation.Session;
using IqraInfrastructure.Managers.Conversation.Session.Agent.AI;
using IqraInfrastructure.Managers.Conversation.Session.Agent.AI.Helpers;
using IqraInfrastructure.Managers.Conversation.Session.Client;
using IqraInfrastructure.Managers.Conversation.Session.Client.Telephony;
using IqraInfrastructure.Managers.Conversation.Session.Client.Transport;
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
using PhoneNumbers;
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
        private readonly OutboundCallCampaignRepository _outboundCallCampaignRepository;
        private readonly ConversationStateRepository _conversationStateRepository;
        private readonly BusinessManager _businessManager;
        private readonly IntegrationsManager _integrationsManager;
        private readonly RegionManager _regionManager;
        private readonly BillingUsageManager _billingProcessingManager;

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
            OutboundCallCampaignRepository outboundCallCampaignRepository,
            ConversationStateRepository conversationStateRepository,
            BusinessManager businessManager,
            IntegrationsManager integrationsManager,
            RegionManager regionManager,
            BillingUsageManager billingProcessingManager
        )
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _backendAppConfig = backendAppConfig;
            _serverMetricsMonitor = serverMetricsMonitor;
            _inboundCallQueueRepository = inboundCallQueueRepository;
            _outboundCallQueueRepository = outboundCallQueueRepository;
            _outboundCallCampaignRepository = outboundCallCampaignRepository;
            _conversationStateRepository = conversationStateRepository;
            _businessManager = businessManager;
            _integrationsManager = integrationsManager;
            _regionManager = regionManager;
            _billingProcessingManager = billingProcessingManager;
        }

        public async Task<FunctionReturnResult<ProcessedInboundCallResponse?>> ProcessInboundCallAsync(string queueId)
        {
            var result = new FunctionReturnResult<ProcessedInboundCallResponse?>();

            FunctionReturnResult<ConversationSession?>? sessionResult = null;

            try
            {
                InboundCallQueueData? inboundQueueData = await _inboundCallQueueRepository.GetInboundCallQueueByIdAsync(queueId);
                if (inboundQueueData == null)
                {
                    return result.SetFailureResult("ProcessInboundCallAsync:QUEUE_NOT_FOUND", "Queue not found");
                }
                await _inboundCallQueueRepository.UpdateInboundCallQueueStatusAsync(queueId, CallQueueStatusEnum.ProcessingBackend);

                int sessionBitPerSample = 16;
                int sessionChannels = 1;
                int sessionSampleRate = 8000;
                AudioEncodingTypeEnum sessionAudioEncodingType = AudioEncodingTypeEnum.PCM;
                if (inboundQueueData.RouteNumberProvider == TelephonyProviderEnum.ModemTel)
                {
                    sessionAudioEncodingType = AudioEncodingTypeEnum.PCM;
                    sessionBitPerSample = 16;
                }
                else if (inboundQueueData.RouteNumberProvider == TelephonyProviderEnum.Twilio)
                {
                    sessionAudioEncodingType = AudioEncodingTypeEnum.MULAW;
                    sessionBitPerSample = 8;
                }

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
                        var sessionAIAgentResult = await CreateAIAgentAsync(sessionResult.Data, new ConversationAgentConfiguration() { BitsPerSample = sessionBitPerSample, Channels = sessionChannels, SampleRate = sessionSampleRate, AudioEncodingType = sessionAudioEncodingType });
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

                        var addSessionTelephonyResult = await sessionResult.Data.AddPrimaryClient(primaryTelephonyClient, new ConversationClientConfiguration() { QueueData = inboundQueueData, BitsPerSample = sessionBitPerSample, Channels = sessionChannels, SampleRate = sessionSampleRate, AudioEncodingType = sessionAudioEncodingType });
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

                await sessionResult.Data.UpdateStateAsync(ConversationSessionState.WaitingForPrimaryClient, "Initalized successfully so now waiting for primary telephony client to connect");
                await _inboundCallQueueRepository.UpdateInboundCallQueueSessionIdAndStatusAsync(queueId, sessionResult.Data.SessionId, CallQueueStatusEnum.ProcessedBackend);

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

        public async Task<FunctionReturnResult<InitiateOutboundCallResultModel>> InitiateOutboundCallAsync(string queueId)
        {
            var result = new FunctionReturnResult<InitiateOutboundCallResultModel>();
            var resultData = new InitiateOutboundCallResultModel()
            {
                ShouldRequeue = false
            };

            FunctionReturnResult<ConversationSession?>? sessionResult = null;

            try
            {
                OutboundCallQueueData? outboundQueueData = await _outboundCallQueueRepository.GetOutboundCallQueueByIdAsync(queueId);
                if (outboundQueueData == null)
                {
                    return result.SetFailureResult("InitiateOutboundCallAsync:QUEUE_NOT_FOUND", "Queue not found", resultData);
                }
                await _outboundCallQueueRepository.UpdateCallStatusAsync(queueId, CallQueueStatusEnum.ProcessingBackend, newProcessingServerId: _backendAppConfig.ServerId);

                int sessionBitPerSample = 16;
                int sessionChannels = 1;
                int sessionSampleRate = 8000;
                AudioEncodingTypeEnum sessionAudioEncodingType = AudioEncodingTypeEnum.PCM;
                if (outboundQueueData.CallingNumberProvider == TelephonyProviderEnum.ModemTel)
                {
                    sessionAudioEncodingType = AudioEncodingTypeEnum.PCM;
                    sessionBitPerSample = 16;
                }
                else if (outboundQueueData.CallingNumberProvider == TelephonyProviderEnum.Twilio)
                {
                    sessionAudioEncodingType = AudioEncodingTypeEnum.MULAW;
                    sessionBitPerSample = 8;
                }

                var businessNumber = await _businessManager.GetNumberManager().GetBusinessNumberById(outboundQueueData.BusinessId, outboundQueueData.CallingNumberId);
                if (businessNumber == null)
                {
                    return result.SetFailureResult("InitiateOutboundCallAsync:BUSINESS_NUMBER_NOT_FOUND", "Business number not found", resultData);
                }

                var integrationResult = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(outboundQueueData.BusinessId, businessNumber.IntegrationId);
                if (!integrationResult.Success || integrationResult.Data == null)
                {
                    return result.SetFailureResult($"InitiateOutboundCallAsync:{integrationResult.Code}", integrationResult.Message, resultData);
                }

                var regionData = await _regionManager.GetRegionById(_backendAppConfig.RegionId);
                if (regionData == null)
                {
                    return result.SetFailureResult("InitiateOutboundCallAsync:REGION_NOT_FOUND", "Region not found", resultData);
                }
                var regionServerData = regionData.Servers.FirstOrDefault(s => s.Endpoint == _backendAppConfig.ServerId);
                if (regionServerData == null)
                {
                    return result.SetFailureResult("InitiateOutboundCallAsync:REGION_SERVER_NOT_FOUND", "Region server not found", resultData);
                }
                var anyRegionProxyServerData = regionData.Servers.FirstOrDefault(s => s.Type == ServerTypeEnum.Proxy);
                if (anyRegionProxyServerData == null)
                {
                    resultData.ShouldRequeue = true;
                    return result.SetFailureResult("InitiateOutboundCallAsync:REGION_PROXY_SERVER_NOT_FOUND", "Region proxy server not found", resultData);
                }

                sessionResult = await CreateConversationSessionAsync(outboundQueueData);
                if (!sessionResult.Success)
                {
                    // CHECK WHY THEN REQUEUE
                    resultData.ShouldRequeue = true;
                    return result.SetFailureResult("InitiateOutboundCallAsync:SESSION_CREATION_FAILED", sessionResult.Message, resultData);
                }
                var startSessionResult = await sessionResult.Data.InitalizeAsync();
                if (!startSessionResult.Success)
                {
                    // CHECK WHY THEN REQUEUE
                    resultData.ShouldRequeue = true;
                    return result.SetFailureResult("InitiateOutboundCallAsync:SESSION_INIT_FAILED", startSessionResult.Message, resultData);
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
                        var sessionAIAgentResult = await CreateAIAgentAsync(sessionResult.Data, new ConversationAgentConfiguration() { BitsPerSample = sessionBitPerSample, Channels = sessionChannels, SampleRate = sessionSampleRate, AudioEncodingType = sessionAudioEncodingType });
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

                        var addSessionTelephonyResult = await sessionResult.Data.AddPrimaryClient(primaryTelephonyClient, new ConversationClientConfiguration() { QueueData = outboundQueueData, BitsPerSample = sessionBitPerSample, Channels = sessionChannels, SampleRate = sessionSampleRate, AudioEncodingType = sessionAudioEncodingType });
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
                });

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

                    await sessionResult.Data.EndAsync("InitiateOutboundCallAsync Failed", ConversationSessionState.Error);
                    await CleanupSessionAsync(sessionResult.Data.SessionId);

                    resultData.ShouldRequeue = true;
                    return result.SetFailureResult("InitiateOutboundCallAsync:SESSION_CREATION_FAILED", "Session creation failed", resultData);
                }

                var generatedWebhookToken = CallWebsocketTokenGenerator.GenerateHmacToken(sessionResult.Data.SessionId, primaryTelephonyClient.ClientId, TimeSpan.FromMinutes(5), _backendAppConfig.WebhookTokenSecret);
                var webhookUrl = BuildWebhookUrl(regionServerData, sessionResult.Data.SessionId, primaryTelephonyClient.ClientId, generatedWebhookToken);
                var callbackUrl = BuildStatusCallbackUrl(anyRegionProxyServerData, outboundQueueData.BusinessId, sessionResult.Data.SessionId, businessNumber.Provider, outboundQueueData.CallingNumberId);

                await sessionResult.Data.UpdateStateAsync(ConversationSessionState.WaitingForPrimaryClient, "Initalized successfully so now waiting for primary telephony client to connect");

                OutboundCallResultModel? callResultModel = null;
                switch (businessNumber.Provider)
                {
                    case TelephonyProviderEnum.ModemTel:
                        {
                            callResultModel = await InitiateModemTelOutboundCallAsync(
                                businessNumber as BusinessNumberModemTelData,
                                integrationResult.Data,
                                outboundQueueData.RecipientNumber,
                                sessionResult.Data.SessionId,
                                webhookUrl,
                                callbackUrl
                            );

                            if (!callResultModel.Success)
                            {
                                return result.SetFailureResult("InitiateOutboundCallAsync:CALL_FAILED", callResultModel.Message, resultData);
                            }

                            break;
                        }

                    case TelephonyProviderEnum.Twilio:
                        {
                            callResultModel = await InitiateTwilioOutboundCallAsync(
                                businessNumber as BusinessNumberTwilioData,
                                integrationResult.Data,
                                outboundQueueData.RecipientNumber,
                                queueId,
                                webhookUrl,
                                callbackUrl
                            );

                            if (!callResultModel.Success)
                            {
                                return result.SetFailureResult("InitiateOutboundCallAsync:CALL_FAILED", callResultModel.Message, resultData);
                            }

                            break;
                        }

                    default:
                        return result.SetFailureResult("InitiateOutboundCallAsync:INVALID_PROVIDER", "Invalid number provider", resultData);
                }

                await sessionResult.Data.StartAsync();

                await _outboundCallQueueRepository.UpdateOutboundCallQueueSessionIdAndStatusAsync(queueId, sessionResult.Data.SessionId, CallQueueStatusEnum.ProcessedBackend);
                return result.SetSuccessResult(result.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "InitiateOutboundCallAsync:EXCEPTION",
                    $"{ex.Message} {ex.Source} {ex.StackTrace}",
                    resultData
                );
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
                            if (sessionData.State == ConversationSessionState.WaitingForPrimaryClient)
                            {
                                await sessionData.StartAsync();
                            }

                            if (sessionData.State == ConversationSessionState.Active)
                            {
                                await sessionData.NotifyConversationStarted();
                            }
                            
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
                catch (ObjectDisposedException) { /** ignore **/ }
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
                    _outboundCallCampaignRepository,
                    _conversationStateRepository,
                    _serviceProvider.GetRequiredService<ConversationAudioRepository>(),
                    _billingProcessingManager,
                    _serviceProvider.GetRequiredService<ILoggerFactory>()
                );

                _activeSessions[sessionId] = conversationSession;
                _ctsSessions[sessionId] = newSessionCTS;

                conversationSession.SessionEnded += async (object? sender, object? e) => { await CleanupSessionAsync(sessionId); };

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
            string customerNumber;
            if (queueData.Type == CallQueueTypeEnum.Inbound)
            {
                var inboundCallQueueData = queueData as InboundCallQueueData;
                clientId = $"{inboundCallQueueData.RouteNumberId}";
                numberId = inboundCallQueueData.RouteNumberId;
                callId = inboundCallQueueData.ProviderCallId;
                customerNumber = inboundCallQueueData.CallerNumber;
            }
            else if (queueData.Type == CallQueueTypeEnum.Outbound)
            {
                var outboundCallQeueData = queueData as OutboundCallQueueData;
                clientId = $"{outboundCallQeueData.CallingNumberId}";
                numberId = outboundCallQeueData.CallingNumberId;
                callId = null;
                customerNumber = outboundCallQeueData.RecipientNumber;
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

            string phoneNumberData = $"+{PhoneNumberUtil.GetInstance().GetCountryCodeForRegion(businessNumberData.CountryCode)}{businessNumberData.Number}";

            switch (businessNumberData.Provider)
            {
                case TelephonyProviderEnum.ModemTel:
                    {
                        var deferredTransport = new DeferredClientTransport(_serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<DeferredClientTransport>());
                        return result.SetSuccessResult(
                            new ModemTelConversationClient(
                                clientId,
                                phoneNumberData,
                                ((BusinessNumberModemTelData)businessNumberData).ModemTelPhoneNumberId,
                                customerNumber,
                                callId,
                                integrationData.Data.Fields["endpoint"],
                                _integrationsManager.DecryptField(integrationData.Data.EncryptedFields["apikey"]),
                                _serviceProvider.GetRequiredService<ModemTelManager>(),
                                deferredTransport,
                                _serviceProvider.GetRequiredService<ILogger<ModemTelConversationClient>>()
                            )
                        );
                    }

                case TelephonyProviderEnum.Twilio:
                    {
                        var deferredTransport = new DeferredClientTransport(_serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<DeferredClientTransport>());
                        return result.SetSuccessResult(
                            new TwilioConversationClient(
                                clientId,
                                phoneNumberData,
                                ((BusinessNumberTwilioData)businessNumberData).TwilioPhoneNumberId,
                                customerNumber,
                                callId,
                                integrationData.Data.Fields["sid"],
                                _integrationsManager.DecryptField(integrationData.Data.EncryptedFields["auth"]),
                                _serviceProvider.GetRequiredService<TwilioManager>(),
                                deferredTransport,
                                _serviceProvider.GetRequiredService<ILogger<TwilioConversationClient>>()
                            )
                        );
                    }

                default:
                    return result.SetFailureResult("CreateTelephonyClient:INVALID_PROVIDER", "Invalid provider");
            }
        }
        private async Task<FunctionReturnResult<IConversationAgent?>> CreateAIAgentAsync(ConversationSession sessionManager, ConversationAgentConfiguration agentConfiguration)
        {
            var result = new FunctionReturnResult<IConversationAgent?>();

            // Create agent ID
            string agentId = $"{Guid.NewGuid().ToString()}";
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
                    _serviceProvider.GetRequiredService<BusinessAgentAudioRepository>(),
                    _serviceProvider.GetRequiredService<IntegrationsManager>(),
                    _serviceProvider.GetRequiredService<ModemTelManager>(),
                    _serviceProvider.GetRequiredService<TwilioManager>(),
                    _serviceProvider.GetRequiredService<TTSAudioCacheManager>()
                );

                return result.SetSuccessResult(AIAgent);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("CreateAIAgentAsync:EXCEPTION", $"Failed to create AI agent: {ex.Message}");
            }
        }

        private async Task<OutboundCallResultModel> InitiateModemTelOutboundCallAsync(BusinessNumberModemTelData phoneNumber, BusinessAppIntegration integration, string toNumber, string sessionId, string websocketUrl, string statusCallbackUrl)
        {
            var result = new OutboundCallResultModel();
            try
            {
                string apiKey = _integrationsManager.DecryptField(integration.EncryptedFields["apikey"]);
                string apiBaseUrl = integration.Fields["endpoint"];
                var modemTelManager = _serviceProvider.GetRequiredService<ModemTelManager>();

                var callResult = await modemTelManager.MakeCallAsync(
                    apiKey,
                    apiBaseUrl,
                    phoneNumber.ModemTelPhoneNumberId,
                    toNumber,
                    statusCallbackUrl,
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
        private async Task<OutboundCallResultModel> InitiateTwilioOutboundCallAsync(BusinessNumberTwilioData phoneNumber, BusinessAppIntegration integration, string toNumber, string sessionId, string websocketUrl, string statusCallbackUrl)
        {
            var result = new OutboundCallResultModel();
            try
            {
                string accountSid = integration.Fields["sid"];
                string authToken = _integrationsManager.DecryptField(integration.EncryptedFields["auth"]);
                var twilioManager = _serviceProvider.GetRequiredService<TwilioManager>();

                var callResult = await twilioManager.MakeCallAsync(
                    accountSid,
                    authToken,
                    phoneNumber.Number,
                    toNumber,
                    statusCallbackUrl, // Status callback for Twilio
                    websocketUrl
                );

                if (!callResult.Success || callResult.Data == null)
                {
                    result.Message = $"Failed to initiate Twilio call: [{callResult.Code}] {callResult.Message}";
                    return result;
                }
                result.Success = true;
                result.CallId = callResult.Data.Sid;
                result.Status = callResult.Data.Status;
                return result;
            }
            catch (Exception ex)
            {
                result.Message = $"Error initiating call: {ex.Message}";
                return result;
            }
        }
        private async Task<OutboundCallResultModel> InitiateSipTrunkOutboundCallAsync(BusinessNumberData sipNumber, string toNumber)
        {
            var result = new OutboundCallResultModel();

            var sipCallManager = _serviceProvider.GetRequiredService<SipCallManager>();

            // 1. Get a new User Agent from our manager
            var uac = sipCallManager.CreateOutboundUserAgent();

            // 2. Create our client wrapper
            var sipClient = new SipConversationClient(
                uac.CallDescriptor.CallId,
                sipNumber.Number, // SIP URI
                $"sip:{toNumber}@{sipNumber.Number}", // PROVIDER DOMAIN
                uac,
                _serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<SipConversationClient>()
            );

            // 3. The CallProcessor will create and manage the session as before...
            // ...
            // var sessionResult = await CreateConversationSessionAsync(...);
            // sessionResult.Data.SetPrimaryClient(sipClient); // pseudo-code

            // 4. Now, tell the client to place the call
            bool callSucceeded = await sipClient.Call($"sip:{toNumber}@{sipNumber.Number}"); // PROVIDER DOMAIN

            if (callSucceeded)
            {
                result.Success = true;
                result.CallId = sipClient.ClientId;
                //result.Client = sipClient; // Pass the active client back
            }
            else
            {
                result.Message = "Failed to initiate SIP call.";
            }

            return result;
        }

        public async Task<FunctionReturnResult<CancellationTokenSource?>> AssignWebSocketToClientAsync(string sessionId, string clientId, string sessionToken, WebSocket webSocket)
        {
            var result = new FunctionReturnResult<CancellationTokenSource?>();

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

                if (convClient is BaseConversationClient baseClient)
                {
                    if (baseClient.Transport is DeferredClientTransport deferredTransport)
                    {
                        var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();

                        var realTransport = new WebSocketClientTransport(
                            webSocket,
                            loggerFactory.CreateLogger<WebSocketClientTransport>(),
                            sessionOverallCts.Token
                        );

                        deferredTransport.Activate(realTransport);

                        if (convClient is TwilioConversationClient twilioClient)
                        {
                            await NotifyTelephonyClientStatus(sessionId, new TelephonyStatusNotifyToBackendModel()
                            {
                                PhoneNumberId = twilioClient.ClientTelephonyProviderPhoneNumberId,
                                Provider = TelephonyProviderEnum.Twilio,
                                Status = "in-progress"
                            });
                        }

                        return result.SetSuccessResult(sessionOverallCts);
                    }
                    else
                    {
                        // This means the client was somehow initialized with a non-deferred transport, or transport is already activated.
                        return result.SetFailureResult("AssignWebSocketToClientAsync:TRANSPORT_MISMATCH", "Client transport is not in a deferred state or is of an unexpected type.");
                    }
                }
                else
                {
                    return result.SetFailureResult("AssignWebSocketToClientAsync:NOT_FOUND", "Conversation client not found or is not a BaseConversationClient.");
                }
            }
            else
            {
                return result.SetFailureResult("AssignWebSocketToClientAsync:SESSION_NOT_FOUND", "Session or session cts not found");
            }
        }

        private string BuildWebhookUrl(RegionServerData serverData, string sessionId, string clientId, string sessionToken)
        {
            var baseURI = new Uri((serverData.UseSSL ? "wss://" : "ws://") + serverData.Endpoint);
            return new Uri(baseURI, $"{(baseURI.AbsolutePath != "/" ? baseURI.AbsolutePath : "")}/ws/session/{sessionId}/client/{clientId}/{sessionToken}").ToString();
        }

        private string BuildStatusCallbackUrl(RegionServerData proxyServerData, long businessId, string sessionId, TelephonyProviderEnum telephonyProvider, string businessNumberId)
        {
            var providerName = telephonyProvider.ToString().ToLower();

            var statusCallbackUrl = new Uri((proxyServerData.UseSSL ? "https://" : "http://") + proxyServerData.Endpoint);
            statusCallbackUrl = new Uri(statusCallbackUrl, $"{(statusCallbackUrl.AbsolutePath != "/" ? statusCallbackUrl.AbsolutePath : "")}/api/{providerName}/webhook/voice/outbound/status/{businessId}/{sessionId}/{businessNumberId}");

            return statusCallbackUrl.ToString();
        }
    }
}