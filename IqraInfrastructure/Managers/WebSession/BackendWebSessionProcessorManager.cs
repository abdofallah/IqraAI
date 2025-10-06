using IqraCore.Entities.Conversation;
using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Region;
using IqraCore.Entities.Server;
using IqraCore.Entities.WebSession;
using IqraCore.Interfaces.Conversation;
using IqraCore.Models.Server;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Call.Backend;
using IqraInfrastructure.Managers.Conversation.Session;
using IqraInfrastructure.Managers.Conversation.Session.Agent.AI;
using IqraInfrastructure.Managers.Conversation.Session.Agent.AI.Helpers;
using IqraInfrastructure.Managers.Conversation.Session.Client;
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
using IqraInfrastructure.Repositories.Conversation;
using IqraInfrastructure.Repositories.WebSession;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace IqraInfrastructure.Managers.WebSession
{
    public class BackendWebSessionProcessorManager
    {
        private BackendAppConfig _backendAppConfig;

        private readonly ILogger<BackendWebSessionProcessorManager> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ServerMetricsMonitor _serverMetricsMonitor;
        private readonly WebSessionRepository _webSessionRepoistory;
        private readonly ConversationStateRepository _conversationStateRepository;
        private readonly BusinessManager _businessManager;
        private readonly IntegrationsManager _integrationsManager;
        private readonly RegionManager _regionManager;
        private readonly UserBillingUsageManager _billingProcessingManager;

        // combine the two
        private readonly ConcurrentDictionary<string, ConversationSessionOrchestrator> _activeSessions = new();
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _ctsSessions = new();

        private readonly SemaphoreSlim _sessionCreationLock = new SemaphoreSlim(1, 1);

        private readonly CancellationTokenSource _processorCTS = new CancellationTokenSource();

        public BackendWebSessionProcessorManager(
            ILogger<BackendWebSessionProcessorManager> logger,
            IServiceProvider serviceProvider,
            BackendAppConfig backendAppConfig,
            ServerMetricsMonitor serverMetricsMonitor,
            WebSessionRepository webSessionRepoistory,
            ConversationStateRepository conversationStateRepository,
            BusinessManager businessManager,
            IntegrationsManager integrationsManager,
            RegionManager regionManager,
            UserBillingUsageManager billingProcessingManager
        )
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _backendAppConfig = backendAppConfig;
            _serverMetricsMonitor = serverMetricsMonitor;
            _webSessionRepoistory = webSessionRepoistory;
            _conversationStateRepository = conversationStateRepository;
            _businessManager = businessManager;
            _integrationsManager = integrationsManager;
            _regionManager = regionManager;
            _billingProcessingManager = billingProcessingManager;
        }

        public async Task<FunctionReturnResult<BackendInitiateWebSessionResultModel?>> InitiateWebSessionConversationAsync(string webSessionId)
        {
            var result = new FunctionReturnResult<BackendInitiateWebSessionResultModel?>();

            FunctionReturnResult<ConversationSessionOrchestrator?>? sessionResult = null;
            try
            {
                WebSessionData? webSessionData = await _webSessionRepoistory.GetWebSessionByIdAsync(webSessionId);
                if (webSessionData == null)
                {
                    return result.SetFailureResult(
                        "InitiateWebSessionConversationAsync:QUEUE_NOT_FOUND",
                        "Web session data not found"
                    );
                }
                await _webSessionRepoistory.UpdateStatusProcessingBackendWithServerId(webSessionId, _backendAppConfig.ServerId);

                var regionData = await _regionManager.GetRegionById(_backendAppConfig.RegionId);
                if (regionData == null)
                {
                    return result.SetFailureResult(
                        "InitiateWebSessionConversationAsync:REGION_NOT_FOUND",
                        "Region not found"
                    );
                }
                var regionServerData = regionData.Servers.FirstOrDefault(s => s.Endpoint == _backendAppConfig.ServerId);
                if (regionServerData == null)
                {
                    return result.SetFailureResult(
                        "InitiateWebSessionConversationAsync:REGION_SERVER_NOT_FOUND",
                        "Region server not found"
                    );
                }

                sessionResult = await CreateConversationSessionAsync(webSessionData);
                if (!sessionResult.Success || sessionResult.Data == null)
                {
                    return result.SetFailureResult(
                        "InitiateWebSessionConversationAsync:SESSION_CREATION_FAILED",
                        $"[{sessionResult.Code}] {sessionResult.Message}"
                    );
                }
                var session = sessionResult.Data;

                var startSessionResult = await session.InitializeAsync();
                if (!startSessionResult.Success)
                {
                    return result.SetFailureResult(
                        "InitiateWebSessionConversationAsync:SESSION_INIT_FAILED",
                        $"[{startSessionResult.Code}] {startSessionResult.Message}"
                    );
                }

                var componentsResult = await BuildAndConfigureSessionAsync(session, webSessionData);
                if (!componentsResult.Success || componentsResult.Data == null)
                {
                    return result.SetFailureResult(
                        "InitiateWebSessionConversationAsync:SESSION_COMPONENTS_FAILED",
                        $"[{componentsResult.Code}] {componentsResult.Message}"
                    );
                }

                var primaryWebSocketClient = componentsResult.Data.Client;

                var generatedWebhookToken = CallWebsocketTokenGenerator.GenerateHmacToken(session.SessionId, primaryWebSocketClient.ClientId, TimeSpan.FromMinutes(5), _backendAppConfig.WebhookTokenSecret);
                var webhookUrl = BuildWebhookUrl(regionServerData, session.SessionId, primaryWebSocketClient.ClientId, generatedWebhookToken);
                
                await session.UpdateStateAsync(ConversationSessionState.WaitingForPrimaryClient, "Initialized successfully, waiting for websocket connection.");

                await _webSessionRepoistory.UpdateStatusProcessedBackendWithServerIdAndWebsocketURL(webSessionId, session.SessionId, webhookUrl);
                return result.SetSuccessResult(
                    new BackendInitiateWebSessionResultModel()
                    {
                        WebSocketURL = webhookUrl
                    }    
                );
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "InitiateWebSessionConversationAsync:EXCEPTION",
                    $"{ex.Message} {ex.Source} {ex.StackTrace}"
                );
            }
            finally
            {
                if (!result.Success && sessionResult?.Data != null)
                {
                    await sessionResult.Data.EndAsync("InitiateWebSessionConversationAsync Failed", ConversationSessionEndType.InitalizeError, ConversationSessionState.Error);
                    await CleanupSessionAsync(sessionResult.Data.SessionId);
                }
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
                        var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();

                        var realTransport = new WebSocketClientTransport(
                            webSocket,
                            loggerFactory.CreateLogger<WebSocketClientTransport>(),
                            sessionOverallCts.Token
                        );
                        deferredTransport.Activate(realTransport);

                        // Start Session
                        if (sessionManager.State == ConversationSessionState.WaitingForPrimaryClient)
                        {
                            await sessionManager.NotifyConversationStarted();
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

        private async Task<FunctionReturnResult<ConversationSessionOrchestrator?>> CreateConversationSessionAsync(WebSessionData webSessionData)
        {
            var result = new FunctionReturnResult<ConversationSessionOrchestrator?>();

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

                var conversationSession = new ConversationSessionOrchestrator(
                    sessionId,
                    ConversationSessionInitiationType.Web,
                    combinedCTS,

                    _businessManager,
                    _conversationStateRepository,
                    _serviceProvider.GetRequiredService<ConversationAudioRepository>(),
                    _billingProcessingManager,
                    _serviceProvider.GetRequiredService<ILoggerFactory>(),

                    webSessionData: webSessionData
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
        private async Task<FunctionReturnResult<SessionComponents>> BuildAndConfigureSessionAsync(ConversationSessionOrchestrator session, WebSessionData webSessionData)
        {
            var result = new FunctionReturnResult<SessionComponents>();

            // TODO in future configurable
            int sessionBitPerSample = 16;
            AudioEncodingTypeEnum sessionAudioEncodingType = AudioEncodingTypeEnum.PCM;
            int sessionChannels = 1;
            int sessionSampleRate = 8000;

            var agentConfig = new ConversationAgentConfiguration()
            {
                BitsPerSample = sessionBitPerSample,
                Channels = sessionChannels,
                SampleRate = sessionSampleRate,
                AudioEncodingType = sessionAudioEncodingType
            };

            var clientConfig = new ConversationWebClientConfiguration()
            {
                WebSessionData = webSessionData,
                BitsPerSample = sessionBitPerSample,
                Channels = sessionChannels,
                SampleRate = sessionSampleRate,
                AudioEncodingType = sessionAudioEncodingType
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
                    await _conversationStateRepository.AddLogEntryAsync(session.SessionId,
                        new ConversationLogEntry
                        {
                            Timestamp = DateTime.UtcNow,
                            Message = $"[BuildAndConfigureSessionAsync:{agentResult.Code}] {agentResult.Message}"
                        });
                    return result.SetFailureResult(agentResult.Code, agentResult.Message);
                }
                agent = agentResult.Data;

                // Create Telephony Client
                var clientResult = await CreateWebSocketClient(webSessionData, session);
                if (!clientResult.Success)
                {
                    await _conversationStateRepository.AddLogEntryAsync(session.SessionId,
                        new ConversationLogEntry
                        {
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
                    await _conversationStateRepository.AddLogEntryAsync(session.SessionId,
                        new ConversationLogEntry
                        {
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
                    await _conversationStateRepository.AddLogEntryAsync(session.SessionId,
                        new ConversationLogEntry
                        {
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
                await _conversationStateRepository.AddLogEntryAsync(session.SessionId,
                    new ConversationLogEntry
                    {
                        Timestamp = DateTime.UtcNow,
                        Message = $"[BuildAndConfigureSessionAsync:EXCEPTION] {ex.Message}"
                    });
                return result.SetFailureResult(
                    "BuildAndConfigureSessionAsync:EXCEPTION",
                    ex.Message
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
        private async Task<FunctionReturnResult<IConversationClient?>> CreateWebSocketClient(WebSessionData webSessionData, ConversationSessionOrchestrator sessionManager)
        {
            var result = new FunctionReturnResult<IConversationClient?>();

            var deferredTransport = new DeferredClientTransport(_serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<DeferredClientTransport>());
            return result.SetSuccessResult(
                new WebAppConversationClient(
                    webSessionData.ClientIdentifier,
                    deferredTransport,
                    _serviceProvider.GetRequiredService<ILogger<WebAppConversationClient>>()
                )
            );
        }
        private async Task<FunctionReturnResult<IConversationAgent?>> CreateAIAgentAsync(ConversationSessionOrchestrator sessionManager, ConversationAgentConfiguration agentConfiguration)
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

        private string BuildWebhookUrl(RegionServerData serverData, string sessionId, string clientId, string sessionToken)
        {
            var baseURI = new Uri((serverData.UseSSL ? "wss://" : "ws://") + serverData.Endpoint);
            return new Uri(baseURI, $"{(baseURI.AbsolutePath != "/" ? baseURI.AbsolutePath : "")}/ws/session/{sessionId}/webclient/{clientId}/{sessionToken}").ToString();
        }
    }

    internal class SessionComponents
    {
        public IConversationAgent Agent { get; init; }
        public IConversationClient Client { get; init; }
    }
}
