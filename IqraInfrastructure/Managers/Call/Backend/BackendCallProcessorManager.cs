using IqraCore.Entities.Billing;
using IqraCore.Entities.Business;
using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.Logs;
using IqraCore.Entities.Conversation.Logs.Enums;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Helper.Server;
using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Region;
using IqraCore.Entities.Server;
using IqraCore.Interfaces.Conversation;
using IqraCore.Interfaces.User;
using IqraCore.Models.Server;
using IqraInfrastructure.Managers.Business;
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
using IqraInfrastructure.Managers.User;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.Call;
using IqraInfrastructure.Repositories.Conversation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using PhoneNumbers;
using SIPSorcery.SIP.App;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace IqraInfrastructure.Managers.Call.Backend
{
    internal class SessionComponents
    {
        public IConversationAgent Agent { get; init; }
        public IConversationClient Client { get; init; }
    }

    public class BackendCallProcessorManager
    {
        private BackendAppConfig _backendAppConfig;

        private readonly ILogger<BackendCallProcessorManager> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ServerMetricsMonitor _serverMetricsMonitor;
        private readonly InboundCallQueueRepository _inboundCallQueueRepository;
        private readonly OutboundCallQueueRepository _outboundCallQueueRepository;
        private readonly OutboundCallQueueGroupRepository _outboundCallCampaignRepository;
        private readonly ConversationStateRepository _conversationStateRepository;
        private readonly ConversationStateLogsRepository _conversationStateLogsRepository;
        private readonly BusinessManager _businessManager;
        private readonly IntegrationsManager _integrationsManager;
        private readonly RegionManager _regionManager;
        private readonly IUserBillingUsageManager _billingProcessingManager;
        private readonly CampaignActionExecutorService _campaignActionExecutorService;
        private readonly IUserUsageValidationManager _userUsageValidationManager;

        // combine the two
        private readonly ConcurrentDictionary<string, ConversationSessionOrchestrator> _activeSessions = new();
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _ctsSessions = new();

        private readonly SemaphoreSlim _sessionCreationLock = new SemaphoreSlim(1, 1);

        private readonly CancellationTokenSource _processorCTS = new CancellationTokenSource();

        public BackendCallProcessorManager(
            ILogger<BackendCallProcessorManager> logger,
            IServiceProvider serviceProvider,
            BackendAppConfig backendAppConfig,
            ServerMetricsMonitor serverMetricsMonitor,
            InboundCallQueueRepository inboundCallQueueRepository,
            OutboundCallQueueRepository outboundCallQueueRepository,
            OutboundCallQueueGroupRepository outboundCallCampaignRepository,
            ConversationStateRepository conversationStateRepository,
            ConversationStateLogsRepository conversationStateLogsRepository,
            BusinessManager businessManager,
            IntegrationsManager integrationsManager,
            RegionManager regionManager,
            IUserBillingUsageManager billingProcessingManager,
            CampaignActionExecutorService campaignActionExecutorService,
            IUserUsageValidationManager userUsageValidationManager
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
            _conversationStateLogsRepository = conversationStateLogsRepository;
            _businessManager = businessManager;
            _integrationsManager = integrationsManager;
            _regionManager = regionManager;
            _billingProcessingManager = billingProcessingManager;
            _campaignActionExecutorService = campaignActionExecutorService;
            _userUsageValidationManager = userUsageValidationManager;
        }

        public async Task<FunctionReturnResult<ProcessedInboundCallResponse?>> ProcessInboundCallAsync(string queueId, InboundCallQueueData? inboundQueueData = null, SIPUserAgent? userAgent = null, SIPServerUserAgent? uas = null)
        {
            var result = new FunctionReturnResult<ProcessedInboundCallResponse?>();

            // Session State
            string sessionId = ObjectId.GenerateNewId().ToString();
            FunctionReturnResult<ConversationSessionOrchestrator?>? sessionResult = null;

            // Call Concurrency State
            bool hasIncreasedCallConcurrency = false;
            long? callQueueBusinessId = null;

            try
            {
                if (inboundQueueData == null)
                {
                    inboundQueueData = await _inboundCallQueueRepository.GetInboundCallQueueByIdAsync(queueId);
                    if (inboundQueueData == null)
                    {
                        return result.SetFailureResult(
                            "ProcessInboundCallAsync:QUEUE_NOT_FOUND",
                            "Queue not found"
                        );
                    }
                }
                await _inboundCallQueueRepository.UpdateInboundCallQueueStatusAsync(queueId, CallQueueStatusEnum.ProcessingBackend);

                if (inboundQueueData.RouteNumberProvider == TelephonyProviderEnum.SIP && (userAgent == null || uas == null))
                {
                    return result.SetFailureResult(
                        "ProcessInboundCallAsync:SIP_SERVER_USER_AGENT_NOT_FOUND",
                        "SIP server user agent not found"
                    );
                }

                RegionData? currentRegionData = await _regionManager.GetRegionById(_backendAppConfig.RegionId);
                if (currentRegionData == null)
                {
                    return result.SetFailureResult(
                        "ProcessInboundCallAsync:REGION_NOT_FOUND",
                        "Region not found"
                    );
                }
                RegionServerData? regionServerData = currentRegionData.Servers.FirstOrDefault(x => x.Id == _backendAppConfig.Id);
                if (regionServerData == null)
                {
                    return result.SetFailureResult(
                        "ProcessInboundCallAsync:REGION_SERVER_NOT_FOUND",
                        "Region server not found"
                    );
                }

                var tryIncreaseCallConcurrency = await _userUsageValidationManager.TryIncreaseUsageConcurrency(inboundQueueData.BusinessId, BillingFeatureKey.CallConcurrency, sessionId, inboundQueueData.Id);
                if (!tryIncreaseCallConcurrency.Success)
                {
                    return result.SetFailureResult(
                        "ProcessInboundCallAsync:" + tryIncreaseCallConcurrency.Code,
                        tryIncreaseCallConcurrency.Message
                    );
                }
                hasIncreasedCallConcurrency = true;
                callQueueBusinessId = inboundQueueData.BusinessId;

                // --- Refactored Block Start ---
                sessionResult = await CreateConversationSessionAsync(inboundQueueData, sessionId);
                if (!sessionResult.Success)
                {
                    return result.SetFailureResult(
                        "ProcessInboundCallAsync:SESSION_CREATION_FAILED",
                        sessionResult.Message
                    );
                }
                var session = sessionResult.Data;

                var startSessionResult = await session.InitializeAsync();
                if (!startSessionResult.Success)
                {
                    return result.SetFailureResult(
                        "ProcessInboundCallAsync:SESSION_INIT_FAILED",
                        startSessionResult.Message
                    );
                }

                var componentsResult = await BuildAndConfigureSessionAsync(session, inboundQueueData, userAgent, uas);
                if (!componentsResult.Success)
                {
                    return result.SetFailureResult(
                        "ProcessInboundCallAsync:SESSION_COMPONENTS_FAILED",
                        componentsResult.Message
                    );
                }

                var primaryTelephonyClient = componentsResult.Data.Client;

                string webhookUrl = "iqra.bot";
                if (inboundQueueData.RouteNumberProvider != TelephonyProviderEnum.SIP)
                {
                    var generatedWebhookToken = CallWebsocketTokenGenerator.GenerateHmacToken(session.SessionId, primaryTelephonyClient.ClientId, TimeSpan.FromMinutes(5), _backendAppConfig.WebhookTokenSecret);
                    webhookUrl = BuildWebhookUrl(regionServerData, session.SessionId, primaryTelephonyClient.ClientId, generatedWebhookToken);
                }

                // --- Refactored Block End ---
                await session.UpdateStateAsync(ConversationSessionState.WaitingForPrimaryClient, "Initialized successfully so now waiting for primary telephony client to connect");
                await _inboundCallQueueRepository.UpdateInboundCallQueueSessionIdAndStatusAsync(
                    queueId,
                    session.SessionId,
                    CallQueueStatusEnum.ProcessedBackend,
                    DateTime.UtcNow
                );

                return result.SetSuccessResult(
                    new ProcessedInboundCallResponse()
                    {
                        SessionId = session.SessionId,
                        WebhookUrl = webhookUrl.ToString() 
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing inbound call");
                return result.SetFailureResult(
                    "ProcessInboundCallAsync:EXCEPTION",
                    $"Error processing inbound call: {ex.Message}"
                );
            }
            finally
            {
                if (!result.Success)
                {
                    if (sessionResult?.Data != null)
                    {
                        await sessionResult.Data.EndAsync("ProcessInboundCall Failed", ConversationSessionEndType.InitalizeError, ConversationSessionState.Error);
                        await CleanupSessionAsync(sessionResult.Data.SessionId);
                    }

                    if (hasIncreasedCallConcurrency && callQueueBusinessId != null)
                    {
                        await _userUsageValidationManager.DecreaseUsageConcurrency(callQueueBusinessId.Value, BillingFeatureKey.CallConcurrency, sessionId, queueId);
                    }
                }
            }
        }
        public async Task<FunctionReturnResult<BackendInitiateOutboundCallResultModel>> InitiateOutboundCallAsync(string queueId)
        {
            var result = new FunctionReturnResult<BackendInitiateOutboundCallResultModel>();
            var resultData = new BackendInitiateOutboundCallResultModel()
            {
                ShouldRequeue = false
            };

            // Session State
            string sessionId = ObjectId.GenerateNewId().ToString();     
            FunctionReturnResult<ConversationSessionOrchestrator?>? sessionResult = null;

            // Call Concurrency State
            bool hasIncreasedCallConcurrency = false;
            long? callQueueBusinessId = null;

            try
            {
                OutboundCallQueueData? outboundQueueData = await _outboundCallQueueRepository.GetOutboundCallQueueByIdAsync(queueId);
                if (outboundQueueData == null)
                {
                    return result.SetFailureResult(
                        "InitiateOutboundCallAsync:QUEUE_NOT_FOUND",
                        "Queue not found",
                        resultData
                    );
                }
                await _outboundCallQueueRepository.UpdateCallStatusAsync(
                    queueId,
                    CallQueueStatusEnum.ProcessingBackend,
                    new CallQueueLogEntry()
                    {
                        Type = CallQueueLogTypeEnum.Information,
                        Message = "Being processed by backend app..."
                    },
                    newProcessingServerId: _backendAppConfig.Id
                );

                if (!_serverMetricsMonitor.HasCapacity())
                {
                    resultData.ShouldRequeue = true;

                    return result.SetFailureResult(
                        "InitiateOutboundCallAsync:NO_SERVER_CAPACITY",
                        "No capacity available on server",
                        resultData
                    );
                }

                // --- Start of Outbound-Specific Logic ---
                var businessNumber = await _businessManager.GetNumberManager().GetBusinessNumberById(outboundQueueData.BusinessId, outboundQueueData.CallingNumberId);
                if (businessNumber == null)
                {
                    return result.SetFailureResult(
                        "InitiateOutboundCallAsync:BUSINESS_NUMBER_NOT_FOUND",
                        "Business number not found",
                        resultData
                    );
                }

                var integrationResult = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(outboundQueueData.BusinessId, businessNumber.IntegrationId);
                if (!integrationResult.Success || integrationResult.Data == null)
                {
                    return result.SetFailureResult(
                        $"InitiateOutboundCallAsync:{integrationResult.Code}",
                        integrationResult.Message,
                        resultData
                    );
                }

                var regionData = await _regionManager.GetRegionById(_backendAppConfig.RegionId);
                if (regionData == null)
                {
                    return result.SetFailureResult(
                        "InitiateOutboundCallAsync:REGION_NOT_FOUND",
                        "Region not found",
                        resultData
                    );
                }
                var regionServerData = regionData.Servers.FirstOrDefault(s => s.Id == _backendAppConfig.Id);
                if (regionServerData == null)
                {
                    return result.SetFailureResult(
                        "InitiateOutboundCallAsync:REGION_SERVER_NOT_FOUND",
                        "Region server not found",
                        resultData
                    );
                }
                var anyRegionProxyServerData = regionData.Servers.FirstOrDefault(s => s.Type == ServerTypeEnum.Proxy);
                if (anyRegionProxyServerData == null)
                {
                    return result.SetFailureResult(
                        "InitiateOutboundCallAsync:REGION_PROXY_SERVER_NOT_FOUND",
                        "Region proxy server not found",
                        resultData
                    );
                }
                // --- End of Outbound-Specific Logic ---
                
                var tryIncreaseCallConcurrency = await _userUsageValidationManager.TryIncreaseUsageConcurrency(outboundQueueData.BusinessId, BillingFeatureKey.CallConcurrency, sessionId, outboundQueueData.Id);
                if (!tryIncreaseCallConcurrency.Success)
                {
                    if (
                        tryIncreaseCallConcurrency!.Code!.Contains("CONCURRENCY_LIMIT_REACHED") ||
                        tryIncreaseCallConcurrency!.Code!.Contains("USER_CUSTOMER_CONCURRENCY_LIMIT_REACHED")
                    )
                    {
                        resultData.ShouldRequeue = true;
                    }

                    return result.SetFailureResult(
                        "InitiateOutboundCallAsync:" + tryIncreaseCallConcurrency.Code,
                        tryIncreaseCallConcurrency.Message,
                        resultData
                    );
                }
                hasIncreasedCallConcurrency = true;
                callQueueBusinessId = outboundQueueData.BusinessId;

                // --- Start of Refactored Session Setup Block ---
                sessionResult = await CreateConversationSessionAsync(outboundQueueData, sessionId);
                if (!sessionResult.Success || sessionResult.Data == null)
                {
                    return result.SetFailureResult(
                        "InitiateOutboundCallAsync:SESSION_CREATION_FAILED",
                        sessionResult.Message,
                        resultData
                    );
                }
                var session = sessionResult.Data;

                var startSessionResult = await session.InitializeAsync();
                if (!startSessionResult.Success)
                {
                    return result.SetFailureResult(
                        "InitiateOutboundCallAsync:SESSION_INIT_FAILED",
                        startSessionResult.Message,
                        resultData
                    );
                }

                var componentsResult = await BuildAndConfigureSessionAsync(session, outboundQueueData);
                if (!componentsResult.Success || componentsResult.Data == null)
                {
                    return result.SetFailureResult(
                        "InitiateOutboundCallAsync:SESSION_COMPONENTS_FAILED",
                        componentsResult.Message,
                        resultData
                    );
                }

                var primaryTelephonyClient = componentsResult.Data.Client;
                // --- End of Refactored Session Setup Block ---

                var generatedWebhookToken = CallWebsocketTokenGenerator.GenerateHmacToken(session.SessionId, primaryTelephonyClient.ClientId, TimeSpan.FromMinutes(5), _backendAppConfig.WebhookTokenSecret);
                var webhookUrl = BuildWebhookUrl(regionServerData, session.SessionId, primaryTelephonyClient.ClientId, generatedWebhookToken);
                var callbackUrl = BuildStatusCallbackUrl(anyRegionProxyServerData, outboundQueueData.BusinessId, session.SessionId, businessNumber.Provider, outboundQueueData.CallingNumberId);

                await session.UpdateStateAsync(ConversationSessionState.WaitingForPrimaryClient, "Initialized successfully, placing outbound call.");

                OutboundCallResultModel? callResultModel = null;
                switch (businessNumber.Provider)
                {
                    case TelephonyProviderEnum.ModemTel:
                        {
                            callResultModel = await InitiateModemTelOutboundCallAsync(
                                (BusinessNumberModemTelData)businessNumber,
                                integrationResult.Data,
                                outboundQueueData.RecipientNumber,
                                session.SessionId,
                                webhookUrl,
                                callbackUrl
                            );
                            break;
                        }
                    case TelephonyProviderEnum.Twilio:
                        {
                            callResultModel = await InitiateTwilioOutboundCallAsync(
                                (BusinessNumberTwilioData)businessNumber,
                                integrationResult.Data,
                                outboundQueueData.RecipientNumber,
                                queueId,
                                webhookUrl,
                                callbackUrl
                            );
                            break;
                        }
                    default:
                        return result.SetFailureResult(
                            "InitiateOutboundCallAsync:INVALID_PROVIDER",
                            "Invalid number provider",
                            resultData
                        );
                }

                if (callResultModel == null || !callResultModel.Success)
                {
                    return result.SetFailureResult(
                        "InitiateOutboundCallAsync:CALL_FAILED",
                        callResultModel?.Message ?? "Call initiation failed.",
                        resultData
                    );
                }

                await _outboundCallQueueRepository.UpdateOutboundCallQueueSessionIdAndStatusAsync(
                    queueId,
                    session.SessionId,
                    CallQueueStatusEnum.ProcessedBackend,
                    DateTime.UtcNow
                );
                await _outboundCallQueueRepository.AddCallLogAsync(
                    queueId,
                    new CallQueueLogEntry()
                    {
                        Type = CallQueueLogTypeEnum.Information,
                        Message = "Call initiated successfully by the backend.",
                    }
                );

                _ = _campaignActionExecutorService.SendOutboundCallQueueTelephonyCampaignAction(queueId, "Call Inititated");

                return result.SetSuccessResult(resultData);
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
                    if (sessionResult?.Data != null)
                    {
                        await sessionResult.Data.EndAsync("InitiateOutboundCall Failed", ConversationSessionEndType.InitalizeError, ConversationSessionState.Error);
                        await CleanupSessionAsync(sessionResult.Data.SessionId);
                    }

                    if (hasIncreasedCallConcurrency && callQueueBusinessId != null)
                    {
                        await _userUsageValidationManager.DecreaseUsageConcurrency(callQueueBusinessId.Value, BillingFeatureKey.CallConcurrency, sessionId, queueId);
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
                            // it should never be any other state than waiting if we get busy
                            if (sessionData.State != ConversationSessionState.WaitingForPrimaryClient)
                            {
                                return result.SetFailureResult("NotifyTelephonyClientStatus:INVALID_STATE", "Invalid state for busy status");
                            }

                            // TODO we need to check for retry logic of the queue
                            // for outbound calls, we need to end the session and clean it up, check for retry logic and requeue if needed

                            _ = sessionData.EndAsync("Busy outbound call response", ConversationSessionEndType.UserDeclinedOrBusy);
                            return result.SetSuccessResult();
                        }

                    case "no-answer":
                        {
                            // it should never be any other state than waiting if we get no answer
                            if (sessionData.State != ConversationSessionState.WaitingForPrimaryClient)
                            {
                                return result.SetFailureResult("NotifyTelephonyClientStatus:INVALID_STATE", "Invalid state for no answer status");
                            }

                            // TODO we need to check for retry logic of the queue
                            // for outbound calls, we need to end the session and clean it up, check for retry logic and requeue if needed

                            _ = sessionData.EndAsync("No answer outbound call response", ConversationSessionEndType.UserNoAnswer);
                            return result.SetSuccessResult();
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
                        var realTransport = new WebSocketClientTransport(
                            webSocket,
                            sessionManager.SessionLoggerFactory.CreateLogger<WebSocketClientTransport>(),
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
        public async Task<FunctionReturnResult> AnswerPrimarySIPClientAndNotifyStarted(string sessionId)
        {
            var result = new FunctionReturnResult();

            try
            {
                if (_activeSessions.TryGetValue(sessionId, out var sessionManager))
                {
                    var primaryClient = sessionManager.PrimaryClient;
                    if (primaryClient == null)
                    {
                        return result.SetFailureResult("AnswerAndNotifyStartedSIPClient:CLIENT_NOT_FOUND", "Client not found");
                    }

                    if (primaryClient is not SipConversationClient sipClient)
                    {
                        return result.SetFailureResult("AnswerAndNotifyStartedSIPClient:NOT_SIP_CLIENT", "Client is not a SIP client");
                    }
                    else
                    {
                        await sipClient.Answer();
                    }

                    var notifyResult = await sessionManager.NotifyConversationStarted(false);
                    if (!notifyResult.Success)
                    {
                        return result.SetFailureResult($"AnswerAndNotifyStartedSIPClient:{notifyResult.Code}", notifyResult.Message);
                    }

                    return result.SetSuccessResult();
                }
                else
                {
                    return result.SetFailureResult("AnswerAndNotifyStartedSIPClient:SESSION_NOT_FOUND", "Session not found");
                }
            }
            catch (Exception ex) {
                return result.SetFailureResult("AnswerAndNotifyStartedSIPClient:EXCEPTION", ex.Message);
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
        
        private async Task<FunctionReturnResult<ConversationSessionOrchestrator?>> CreateConversationSessionAsync(CallQueueData queueData, string sessionId)
        {
            var result = new FunctionReturnResult<ConversationSessionOrchestrator?>();

            try
            {
                await _sessionCreationLock.WaitAsync(_processorCTS.Token);    
                CancellationTokenSource newSessionCTS = new CancellationTokenSource();
                CancellationTokenSource combinedCTS = CancellationTokenSource.CreateLinkedTokenSource(newSessionCTS.Token, _processorCTS.Token);

                var conversationSession = new ConversationSessionOrchestrator(
                    sessionId,
                    ConversationSessionInitiationType.Telephony,
                    combinedCTS,

                    _businessManager,
                    _conversationStateRepository,
                    _conversationStateLogsRepository,
                    _serviceProvider.GetRequiredService<BusinessConversationAudioRepository>(),
                    _billingProcessingManager,
                    _serviceProvider.GetRequiredService<ILoggerFactory>(),
                    _campaignActionExecutorService,
                    _serviceProvider.GetRequiredService<LLMProviderManager>(),

                    queueData: queueData
                );

                _activeSessions[sessionId] = conversationSession;
                _ctsSessions[sessionId] = newSessionCTS;

                conversationSession.SessionEnded += async (sessionDataAsSender) => {
                    if (sessionDataAsSender is ConversationSessionOrchestrator sessionOrchestrator)
                    {
                        if (sessionOrchestrator.IsCallInitiated)
                        {
                            if (sessionOrchestrator.IsOutboundCall)
                            {
                                await _userUsageValidationManager.DecreaseUsageConcurrency(
                                    sessionOrchestrator.BusinessData!.MasterUserEmail,
                                    sessionOrchestrator.BusinessData!.Id,
                                    BillingFeatureKey.CallConcurrency,
                                    sessionOrchestrator.SessionId,
                                    sessionOrchestrator.CallQueueData!.Id
                                );
                            }
                            else if (sessionOrchestrator.IsInboundCall)
                            {
                                await _userUsageValidationManager.DecreaseUsageConcurrency(
                                    sessionOrchestrator.BusinessData!.MasterUserEmail,
                                    sessionOrchestrator.BusinessData!.Id,
                                    BillingFeatureKey.CallConcurrency,
                                    sessionOrchestrator.SessionId,
                                    sessionOrchestrator.CallQueueData!.Id
                                );
                            }
                        }                    
                    }

                    await CleanupSessionAsync(sessionId);
                };

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
        private async Task<FunctionReturnResult<SessionComponents>> BuildAndConfigureSessionAsync(ConversationSessionOrchestrator session, CallQueueData queueData, SIPUserAgent? userAgent = null, SIPServerUserAgent? uas = null)
        {
            var result = new FunctionReturnResult<SessionComponents>();

            // 1. Determine Audio Configuration from Provider
            TelephonyProviderEnum provider = queueData is InboundCallQueueData inbound
                ? inbound.RouteNumberProvider
                : ((OutboundCallQueueData)queueData).CallingNumberProvider;

            int sessionChannels = 1;
            int sessionSampleRate = 8000;
            int sessionBitPerSample;
            AudioEncodingTypeEnum sessionAudioEncodingType;

            switch (provider)
            {
                case TelephonyProviderEnum.ModemTel:
                    sessionAudioEncodingType = AudioEncodingTypeEnum.PCM;
                    sessionBitPerSample = 16;
                    break;
                case TelephonyProviderEnum.Twilio:
                    sessionAudioEncodingType = AudioEncodingTypeEnum.MULAW;
                    sessionBitPerSample = 8;
                    break;
                case TelephonyProviderEnum.SIP:
                    // todo get from user config
                    sessionAudioEncodingType = AudioEncodingTypeEnum.PCM;
                    sessionSampleRate = 16000;
                    sessionBitPerSample = 16;
                    break;
                default:
                    return result.SetFailureResult(
                        "BuildAndConfigureSessionAsync:UNSUPPORTED_PROVIDER",
                        $"Unsupported telephony provider: {provider}"
                    );
            }

            var agentConfig = new ConversationAgentConfiguration()
            {
            };

            var clientConfig = new ConversationTelephonyClientConfiguration()
            {
                QueueData = queueData,
                AudioInputConfiguration = new ConversationClientAudioInputConfiguration()
                {
                    AudioEncodingType = sessionAudioEncodingType,
                    BitsPerSample = sessionBitPerSample,
                    Channels = sessionChannels,
                    SampleRate = sessionSampleRate
                },
                AudioOutputConfiguration = new ConversationClientAudioOutputConfiguration()
                {
                    AudioEncodingType = sessionAudioEncodingType,
                    BitsPerSample = sessionBitPerSample,
                    Channels = sessionChannels,
                    SampleRate = sessionSampleRate,
                    FrameDurationMs = 30, // for opus
                }
            };

            // 2. Create and Add Agent and Client within a Task to isolate logic
            IConversationAgent? agent = null;
            IConversationClient? client = null;
            bool success = false;

            try
            {
                // Create AI Agent
                var agentResult = await CreateAIAgentAsync(session, agentConfig);
                if (!agentResult.Success)
                {
                    await _conversationStateLogsRepository.AddLogEntryAsync(session.SessionId,
                        new ConversationStateLogEntry {
                            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                            Timestamp = DateTime.UtcNow,
                            Message = $"[BuildAndConfigureSessionAsync:{agentResult.Code}] {agentResult.Message}"
                        });
                    return result.SetFailureResult(agentResult.Code, agentResult.Message);
                }
                agent = agentResult.Data;

                // Create Telephony Client
                var clientResult = await CreateTelephonyClient(queueData, session, clientConfig, userAgent, uas);
                if (!clientResult.Success)
                {
                    await _conversationStateLogsRepository.AddLogEntryAsync(session.SessionId,
                        new ConversationStateLogEntry {
                            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                            Timestamp = DateTime.UtcNow,
                            Message = $"[BuildAndConfigureSessionAsync:{clientResult.Code}] {clientResult.Message}"
                        });
                    agent?.Dispose(); // Clean up the successfully created agent
                    return result.SetFailureResult(clientResult.Code, clientResult.Message);
                }
                client = clientResult.Data;

                // Add Agent to Session
                var addAgentResult = await session.AddPrimaryAgent(agent);
                if (!addAgentResult.Success)
                {
                    await _conversationStateLogsRepository.AddLogEntryAsync(session.SessionId,
                        new ConversationStateLogEntry {
                            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                            Timestamp = DateTime.UtcNow,
                            Message = $"[BuildAndConfigureSessionAsync:{addAgentResult.Code}] {addAgentResult.Message}"
                        });
                    agent.Dispose();
                    client.Dispose();
                    return result.SetFailureResult(addAgentResult.Code, addAgentResult.Message);
                }

                // Add Client to Session
                var addClientResult = await session.AddPrimaryClient(client, clientConfig);
                if (!addClientResult.Success)
                {
                    await _conversationStateLogsRepository.AddLogEntryAsync(session.SessionId,
                        new ConversationStateLogEntry {
                            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                            Timestamp = DateTime.UtcNow,
                            Message = $"[BuildAndConfigureSessionAsync:{addClientResult.Code}] {addClientResult.Message}"
                        });
                    // The session will own the agent now, but the client failed to be added.
                    client.Dispose();
                    return result.SetFailureResult(addClientResult.Code, addClientResult.Message);
                }

                success = true;
                return result.SetSuccessResult(new SessionComponents { Agent = agent, Client = client });
            }
            catch (Exception ex)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(session.SessionId,
                    new ConversationStateLogEntry {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Timestamp = DateTime.UtcNow,
                        Message = $"[BuildAndConfigureSessionAsync:EXCEPTION] {ex.StackTrace} {ex.Message}"
                    });
                return result.SetFailureResult(
                    "BuildAndConfigureSessionAsync:EXCEPTION",
                    $"{ex.Message}"
                );
            }
            finally
            {
                if (!success)
                {
                    // If anything failed, we attempt to dispose of any components that were created
                    // but not successfully attached to the session (which would handle their disposal).
                    // The AddPrimaryAgent/Client results handle more granular cleanup.
                    agent?.Dispose();
                    client?.Dispose();
                }
            }
        }
        private async Task<FunctionReturnResult<IConversationClient?>> CreateTelephonyClient(CallQueueData queueData, ConversationSessionOrchestrator sessionManager, ConversationClientConfiguration clientConfig, SIPUserAgent? userAgent = null, SIPServerUserAgent? uas = null)
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
                        var deferredTransport = new DeferredClientTransport(sessionManager.SessionLoggerFactory.CreateLogger<DeferredClientTransport>());
                        return result.SetSuccessResult(
                            new ModemTelConversationClient(
                                sessionManager.SessionId,
                                clientId,
                                clientConfig,
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
                        var deferredTransport = new DeferredClientTransport(sessionManager.SessionLoggerFactory.CreateLogger<DeferredClientTransport>());
                        return result.SetSuccessResult(
                            new TwilioConversationClient(
                                sessionManager.SessionId,
                                clientId,
                                clientConfig,
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

                case TelephonyProviderEnum.SIP:
                    {
                        if (userAgent == null || uas == null)
                        {
                            return result.SetFailureResult("CreateTelephonyClient:SIP_SERVER_USER_AGENT_NOT_FOUND", "SIP server user agent not found");
                        }

                        var deferredTransport = new DeferredClientTransport(sessionManager.SessionLoggerFactory.CreateLogger<DeferredClientTransport>());
                        return result.SetSuccessResult(
                            new SipConversationClient(
                                sessionManager.SessionId,
                                clientId,
                                clientConfig,
                                ((BusinessNumberSipData)businessNumberData).Number,
                                numberId,
                                customerNumber,
                                userAgent,
                                uas,
                                deferredTransport,
                                _serviceProvider.GetRequiredService<ILogger<SipConversationClient>>()
                            )
                        );
                    }

                default:
                    return result.SetFailureResult("CreateTelephonyClient:INVALID_PROVIDER", "Invalid provider");
            }
        }
        private async Task<FunctionReturnResult<IConversationAgent?>> CreateAIAgentAsync(ConversationSessionOrchestrator sessionManager, ConversationAgentConfiguration agentConfiguration)
        {
            var result = new FunctionReturnResult<IConversationAgent?>();

            // Create agent ID
            string agentId = $"{ObjectId.GenerateNewId().ToString()}";
            try
            {
                var AIAgent = new ConversationAIAgent(
                    sessionManager.SessionLoggerFactory,
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
                    _serviceProvider.GetRequiredService<TTSAudioCacheManager>(),
                    _serviceProvider
                );

                await AIAgent.InitializeAsync();

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
        private async Task<OutboundCallResultModel> InitiateSipTrunkOutboundCallAsync(BusinessNumberData sipNumber, string toNumber, ConversationClientConfiguration clientConfig)
        {
            var result = new OutboundCallResultModel();

            //var sipCallManager = _serviceProvider.GetRequiredService<SipCallManager>();

            //// 1. Get a new User Agent from our manager
            //var uac = sipCallManager.CreateOutboundUserAgent();

            //// 2. Create our client wrapper
            //var sipClient = new SipConversationClient(
            //    uac.CallDescriptor.CallId,
            //    null, // TODOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO IMPORTANT
            //    sipNumber.Number, // SIP URI
            //    $"sip:{toNumber}@{sipNumber.Number}", // PROVIDER DOMAIN
            //    uac,
            //    _serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<SipConversationClient>()
            //);

            //// 3. The CallProcessor will create and manage the session as before...
            //// ...
            //// var sessionResult = await CreateConversationSessionAsync(...);
            //// sessionResult.Data.SetPrimaryClient(sipClient); // pseudo-code

            //// 4. Now, tell the client to place the call
            //bool callSucceeded = await sipClient.Call($"sip:{toNumber}@{sipNumber.Number}"); // PROVIDER DOMAIN

            //if (callSucceeded)
            //{
            //    result.Success = true;
            //    result.CallId = sipClient.ClientId;
            //    //result.Client = sipClient; // Pass the active client back
            //}
            //else
            //{
            //    result.Message = "Failed to initiate SIP call.";
            //}

            return result;
        }
    
        private string BuildWebhookUrl(RegionServerData serverData, string sessionId, string clientId, string sessionToken)
        {
            var baseURI = new Uri((serverData.UseSSL ? "wss://" : "ws://") + serverData.Endpoint);
            return new Uri(baseURI, $"{(baseURI.AbsolutePath != "/" ? baseURI.AbsolutePath : "")}/ws/session/{sessionId}/telephonyclient/{clientId}/{sessionToken}").ToString();
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