using IqraCore.Entities.Billing;
using IqraCore.Entities.Business;
using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Conversation;
using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Conversation.Context;
using IqraCore.Entities.Conversation.Context.Action;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Conversation.Logs;
using IqraCore.Entities.Conversation.Logs.Enums;
using IqraCore.Entities.Conversation.Turn;
using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User.Usage.Enums;
using IqraCore.Entities.WebSession;
using IqraCore.Interfaces.Conversation;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Call;
using IqraInfrastructure.Managers.Conversation.Session.Agent.AI;
using IqraInfrastructure.Managers.Conversation.Session.Client.Telephony;
using IqraInfrastructure.Managers.Conversation.Session.Helpers;
using IqraInfrastructure.Managers.Conversation.Session.Logger;
using IqraInfrastructure.Managers.User;
using IqraInfrastructure.Repositories.Conversation;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Conversation.Session
{
    public class ConversationSessionOrchestrator : IDisposable
    {
        private readonly SessionLoggerFactory _sessionLoggerFactory;
        private readonly ILogger<ConversationSessionOrchestrator> _logger;

        private readonly BusinessManager _businessManager;
        private readonly ConversationStateRepository _conversationStateRepository;
        private readonly ConversationStateLogsRepository _conversationStateLogsRepository;
        private readonly ConversationAudioRepository _audioStorageManager;
        private readonly UserBillingUsageManager _userBillingUsageManager;
        private readonly CampaignActionExecutorService _campaignActionExecutorService;

        private readonly string _sessionId;
        private readonly DateTime _createdAt;
        private readonly ConversationSessionInitiationType _sessionInitiationType;

        private readonly long _sessionBusinessId;
        private readonly string _sessionRegionId;
        private readonly string _sessionRegionProcessingServerId;

        // Telephony Data
        private CallQueueData? _sessionCallQueueData;
        private BusinessAppTelephonyCampaign? _sessionCallQueueTelephonyCampaignData;
        private BusinessAppRoute? _sessionCallQueueRouteData;

        // Web Session Data
        private WebSessionData? _sessionWebSessionData;
        private BusinessAppWebCampaign? _sessionWebSessionCampaignData;

        private readonly List<IConversationClient> _clients = new();
        private IConversationClient? _primaryClient = null;

        private readonly List<IConversationAgent> _agents = new();
        private IConversationAgent? _primaryAgent = null;

        private BusinessData _sessionBusinessData;
        private BusinessApp _sessionBusinessAppData;
        private ConversationSessionContext _sessionContextData;

        private readonly object _clientsLock = new();
        private readonly object _agentsLock = new();

        private ConversationSessionState _state = ConversationSessionState.Created;
        private DateTime _lastUserActivityTime = DateTime.UtcNow;
        private Timer? _silenceTimer;
        private Timer? _sessionDurationTimer;

        private CancellationTokenSource _sessionCts;
        private bool disposedValue;

        public event EventHandler<ConversationSessionStateChangedEventArgs>? StateChanged;
        public event EventHandler<ConversationDTMFReceivedEventArgs>? DTMFRecieved;
        public event EventHandler<ConversationClientAddedEventArgs>? ClientAdded;
        public event EventHandler<ConversationClientRemovedEventArgs>? ClientRemoved;
        public event EventHandler<ConversationAgentAddedEventArgs>? AgentAdded;
        public event EventHandler<ConversationAgentRemovedEventArgs>? AgentRemoved;
        // --- NEW TURN MANAGEMENT ---
        public event EventHandler<ConversationTurnEventArgs>? TurnStarted;
        public event EventHandler<ConversationTurnEventArgs>? TurnUpdated;
        public event EventHandler<ConversationTurnEventArgs>? TurnCompleted;
        // --- END NEW ---

        public event Func<object, Task>? SessionEnded;

        // Public
        public SessionLoggerFactory SessionLoggerFactory => _sessionLoggerFactory;
        public string SessionId => _sessionId;
        public ConversationSessionState State => _state;
        public bool IsCallInitiated => _sessionInitiationType == ConversationSessionInitiationType.Telephony;
        public bool IsInboundCall => IsCallInitiated && (_sessionCallQueueData != null && _sessionCallQueueData.Type == CallQueueTypeEnum.Inbound);
        public bool IsOutboundCall => IsCallInitiated && (_sessionCallQueueData != null && _sessionCallQueueData.Type == CallQueueTypeEnum.Outbound);
        public bool IsWebInitiated => _sessionInitiationType == ConversationSessionInitiationType.Web;
        public BusinessApp? BusinessApp => _sessionBusinessAppData;
        public BusinessData? BusinessData => _sessionBusinessData;
        public CallQueueData? CallQueueData => _sessionCallQueueData;
        public BusinessAppTelephonyCampaign? CallQueueTelephonyCampaignData => _sessionCallQueueTelephonyCampaignData;
        public BusinessAppRoute? CallQueueRouteData => _sessionCallQueueRouteData;
        public WebSessionData? WebSessionData => _sessionWebSessionData;
        public BusinessAppWebCampaign? WebSessionCampaignData => _sessionWebSessionCampaignData;
        public ConversationSessionContext? Context => _sessionContextData;
        public IConversationClient? PrimaryClient => _primaryClient;
        public IConversationAgent? PrimaryAgent => _primaryAgent;
        public CancellationTokenSource CancellationTokenSource => _sessionCts;

        public ConversationSessionOrchestrator(
            string sessionId,
            ConversationSessionInitiationType sessionInitiationType,
            CancellationTokenSource sessionCTS,

            BusinessManager businessManager,
            ConversationStateRepository conversationStateRepository,
            ConversationStateLogsRepository conversationStateLogsRepository,
            ConversationAudioRepository audioStorageManager,
            UserBillingUsageManager billingProcessingManager,
            ILoggerFactory loggerFactory,
            CampaignActionExecutorService campaignActionExecutorService,

            CallQueueData? queueData = null,
            WebSessionData? webSessionData = null
        )
        {
            _sessionId = sessionId;
            _createdAt = DateTime.UtcNow;
            _sessionInitiationType = sessionInitiationType;
            _sessionCts = sessionCTS;

            _businessManager = businessManager;
            _conversationStateRepository = conversationStateRepository;
            _conversationStateLogsRepository = conversationStateLogsRepository;
            _audioStorageManager = audioStorageManager;
            _userBillingUsageManager = billingProcessingManager;

            _sessionLoggerFactory = new SessionLoggerFactory(loggerFactory, _sessionId, _conversationStateLogsRepository);

            _logger = _sessionLoggerFactory.CreateLogger<ConversationSessionOrchestrator>();
            _campaignActionExecutorService = campaignActionExecutorService;

            if (IsCallInitiated)
            {
                if (queueData == null) throw new ArgumentNullException(nameof(queueData));
                _sessionCallQueueData = queueData;
                _sessionBusinessId = queueData.BusinessId;
                _sessionRegionId = queueData.RegionId!;
                _sessionRegionProcessingServerId = queueData.ProcessingBackendServerId!;
            }
            else if (IsWebInitiated)
            {
                if (webSessionData == null) throw new ArgumentNullException(nameof(webSessionData));
                _sessionWebSessionData = webSessionData;
                _sessionBusinessId = webSessionData.BusinessId;
                _sessionRegionId = webSessionData.RegionId;
                _sessionRegionProcessingServerId = webSessionData.SessionRegionBackendServerId!;
            }
        }

        // Initalize
        public async Task<FunctionReturnResult> InitializeAsync()
        {
            var result = new FunctionReturnResult();

            try
            {
                await InitializeConversationConfigurationAsync();

                // Create Conversation State
                var conversationState = new ConversationState
                {
                    Id = _sessionId,
                    BusinessMasterEmail = _sessionBusinessData.MasterUserEmail,
                    BusinessId = _sessionBusinessId,
                    Status = ConversationSessionState.Created,
                    SessionInitiationType = _sessionInitiationType,
                    StartTime = DateTime.UtcNow,
                    ProcessingServerId = _sessionRegionProcessingServerId,
                    RegionId = _sessionRegionId,
                    ExpectedEndTimeAt = DateTime.UtcNow.AddSeconds(_sessionContextData.Timeout.MaxCallTimeS)
                };
                if (IsCallInitiated)
                {
                    conversationState.QueueId = _sessionCallQueueData!.Id;
                }
                else if (IsWebInitiated)
                {
                    conversationState.WebSessionId = _sessionWebSessionData!.Id;
                }

                await _conversationStateRepository.CreateAsync(conversationState);

                _sessionLoggerFactory.ActivateDatabaseLogging();
                _logger.LogInformation("Database logging activated for session {SessionId}.", _sessionId);

                return result.SetSuccessResult();
            }
            catch (Exception ex) {
                return result.SetFailureResult("InitalizeAsync:EXCEPTION", $"Failed to initialize conversation session state: {ex.Message}");
            }
        }
        private async Task InitializeConversationConfigurationAsync()
        {
            var businessData = await _businessManager.GetUserBusinessById(_sessionBusinessId, "InitalizeConversationConfigurationAsync");
            if (!businessData.Success)
            {
                _logger.LogError("Business data not found for business ID {BusinessId}", _sessionBusinessId);
                throw new InvalidOperationException($"Business data not found for business ID {_sessionBusinessId}");
            }
            _sessionBusinessData = businessData.Data;

            var businessAppData = await _businessManager.GetUserBusinessAppById(_sessionBusinessId, "InitalizeConversationConfigurationAsync");
            if (!businessAppData.Success)
            {
                _logger.LogError("Business app data not found for business ID {BusinessId}", _sessionBusinessId);
                throw new InvalidOperationException($"Business app data not found for business ID {_sessionBusinessId}");
            }
            _sessionBusinessAppData = businessAppData.Data;

            if (IsCallInitiated)
            {
                await InitializeTelephonyConversationConfigurationAsync();
            }
            else if (IsWebInitiated)
            {
                await InitializeWebConversationConfigurationAsync();
            }
        }
        private async Task InitializeTelephonyConversationConfigurationAsync()
        {
            if (_sessionCallQueueData.Type == CallQueueTypeEnum.Inbound)
            {
                InboundCallQueueData inboundCallQueue = _sessionCallQueueData as InboundCallQueueData;

                var businessRouteData = _sessionBusinessAppData.Routings.Find(r => r.Id == inboundCallQueue.RouteId);
                if (businessRouteData == null)
                {
                    _logger.LogError("Business route data not found for business ID {BusinessId} and route ID {RouteId}", _sessionBusinessId, inboundCallQueue.RouteId);
                    throw new InvalidOperationException($"Business route data not found for business ID {_sessionBusinessId} and route ID {inboundCallQueue.RouteId}");
                }
                _sessionCallQueueRouteData = businessRouteData;

                _sessionContextData = new ConversationSessionContext()
                {
                    Agent = new ConversationSessionContextAgent()
                    {
                        SelectedAgentId = businessRouteData.Agent.SelectedAgentId,
                        OpeningScriptId = businessRouteData.Agent.OpeningScriptId,
                        TelephonyNumberInContext = businessRouteData.Agent.RouteNumberInContext,
                        CallerNumberInContext = businessRouteData.Agent.CallerNumberInContext,
                        Timezones = businessRouteData.Agent.Timezones
                    },
                    Timeout = new ConversationSessionContextTimeout()
                    {
                        PickUpDelayMS = businessRouteData.Configuration.PickUpDelayMS,
                        NotifyOnSilenceMS = businessRouteData.Configuration.NotifyOnSilenceMS,
                        EndCallOnSilenceMS = businessRouteData.Configuration.EndCallOnSilenceMS,
                        MaxCallTimeS = businessRouteData.Configuration.MaxCallTimeS
                    },
                    Language = new ConversationSessionContextLanguage()
                    {
                        DefaultLanguageCode = businessRouteData.Language.DefaultLanguageCode,
                        MultiLanguageEnabled = businessRouteData.Language.MultiLanguageEnabled,
                        EnabledMultiLanguages = businessRouteData.Language.EnabledMultiLanguages
                    },
                    RecordCallAudio = businessRouteData.Configuration.RecordCallAudio,
                    DynamicVariables = new Dictionary<string, string>(),
                    Metadata = new Dictionary<string, string>()
                };

                // Actions
                if (businessRouteData.Actions != null)
                {
                    // Ended
                    if (businessRouteData.Actions.CallEndedTool != null && businessRouteData.Actions.CallEndedTool.SelectedToolId != null)
                    {
                        _sessionContextData.CallEndedAction = new ConversationSessionContextAction()
                        {
                            SelectedToolId = businessRouteData.Actions.CallEndedTool.SelectedToolId,
                            Arguments = businessRouteData.Actions.CallEndedTool.Arguments ?? new Dictionary<string, object>()
                        };
                    }
                }
            }
            else if (_sessionCallQueueData.Type == CallQueueTypeEnum.Outbound)
            {
                OutboundCallQueueData outboundCallQueue = _sessionCallQueueData as OutboundCallQueueData;
                var campaignDataResult = await _businessManager.GetCampaignManager().GetTelephonyCampaignById(_sessionBusinessData.Id, outboundCallQueue.CampaignId);
                if (!campaignDataResult.Success)
                {
                    _logger.LogError("Outbound call campaign data not found for business ID {BusinessId} and queue ID {RouteId}", _sessionBusinessId, outboundCallQueue.Id);
                    throw new InvalidOperationException($"Outbound call campaign not found for business ID {_sessionBusinessId} and queue ID {outboundCallQueue.Id}");
                }
                _sessionCallQueueTelephonyCampaignData = campaignDataResult.Data;

                _sessionContextData = new ConversationSessionContext()
                {
                    Agent = new ConversationSessionContextAgent()
                    {
                        SelectedAgentId = campaignDataResult.Data.Agent.SelectedAgentId,
                        OpeningScriptId = campaignDataResult.Data.Agent.OpeningScriptId,
                        Timezones = campaignDataResult.Data.Agent.Timezones,
                        TelephonyNumberInContext = campaignDataResult.Data.Agent.FromNumberInContext,
                        RecipientNumberInContext = campaignDataResult.Data.Agent.ToNumberInContext,
                    },
                    Language = new ConversationSessionContextLanguage()
                    {
                        DefaultLanguageCode = campaignDataResult.Data.Agent.Language,
                        MultiLanguageEnabled = false // TODO MULTI LANG
                    },
                    Timeout = new ConversationSessionContextTimeout()
                    {
                        NotifyOnSilenceMS = campaignDataResult.Data.Configuration.Timeouts.NotifyOnSilenceMS,
                        EndCallOnSilenceMS = campaignDataResult.Data.Configuration.Timeouts.EndOnSilenceMS,
                        MaxCallTimeS = campaignDataResult.Data.Configuration.Timeouts.MaxCallTimeS,
                    },
                    DynamicVariables = outboundCallQueue.DynamicVariables,
                    Metadata = outboundCallQueue.Metadata
                };

                // ACTIONS
                if (campaignDataResult.Data.Actions != null)
                {
                    // Ended
                    if (campaignDataResult.Data.Actions.CallEndedTool != null && campaignDataResult.Data.Actions.CallEndedTool.ToolId != null)
                    {
                        _sessionContextData.CallEndedAction = new ConversationSessionContextAction()
                        {
                            SelectedToolId = campaignDataResult.Data.Actions.CallEndedTool.ToolId,
                            Arguments = campaignDataResult.Data.Actions.CallEndedTool.Arguments ?? new Dictionary<string, object>()
                        };
                    }
                }
            }
        }
        private async Task InitializeWebConversationConfigurationAsync()
        {
            var campaignDataResult = await _businessManager.GetCampaignManager().GetWebCampaignById(_sessionBusinessData.Id, _sessionWebSessionData.WebCampaignId);
            if (!campaignDataResult.Success)
            {
                _logger.LogError("Web session campaign data not found for business ID {BusinessId} and web session ID {WebSessionId}", _sessionBusinessId, _sessionWebSessionData.WebCampaignId);
                throw new InvalidOperationException($"Web session campaign not found for business ID {_sessionBusinessId} and queue ID {_sessionWebSessionData.WebCampaignId}");
            }
            _sessionWebSessionCampaignData = campaignDataResult.Data;

            _sessionContextData = new ConversationSessionContext()
            {
                Agent = new ConversationSessionContextAgent()
                {
                    SelectedAgentId = campaignDataResult.Data.Agent.SelectedAgentId,
                    OpeningScriptId = campaignDataResult.Data.Agent.OpeningScriptId,
                    Timezones = campaignDataResult.Data.Agent.Timezones
                },
                Language = new ConversationSessionContextLanguage()
                {
                    DefaultLanguageCode = campaignDataResult.Data.Agent.Language,
                    MultiLanguageEnabled = false // TODO MULTI LANG
                },
                Timeout = new ConversationSessionContextTimeout()
                {
                    NotifyOnSilenceMS = campaignDataResult.Data.Configuration.Timeouts.NotifyOnSilenceMS,
                    EndCallOnSilenceMS = campaignDataResult.Data.Configuration.Timeouts.EndOnSilenceMS,
                    MaxCallTimeS = campaignDataResult.Data.Configuration.Timeouts.MaxConversationTimeS
                },
                DynamicVariables = _sessionWebSessionData.DynamicVariables,
                Metadata = _sessionWebSessionData.Metadata
            };

            // ACTIONS
            if (campaignDataResult.Data.Actions != null)
            {
                // Ended
                if (campaignDataResult.Data.Actions.ConversationEndedTool != null && campaignDataResult.Data.Actions.ConversationEndedTool.ToolId != null)
                {
                    _sessionContextData.CallEndedAction = new ConversationSessionContextAction()
                    {
                        SelectedToolId = campaignDataResult.Data.Actions.ConversationEndedTool.ToolId,
                        Arguments = campaignDataResult.Data.Actions.ConversationEndedTool.Arguments ?? new Dictionary<string, object>()
                    };
                }
            }
        }

        // Clients
        public async Task<FunctionReturnResult> AddPrimaryClient(IConversationClient client, ConversationClientConfiguration clientConfig)
        {
            var result = new FunctionReturnResult();

            if (_primaryClient != null)
            {
                return result.SetFailureResult("AddPrimaryClient:ALREADY_REGISTERED", "A primary client is already registered with the session");
            }

            var addClientResult = await AddClientAsync(client, clientConfig);
            if (!addClientResult)
            {
                return result.SetFailureResult("AddPrimaryClient:ADD_CLIENT_FAILED", "Failed to add primary client");
            }

            _primaryClient = client;

            return result.SetSuccessResult();
        }
        public async Task<bool> AddClientAsync(IConversationClient client, ConversationClientConfiguration clientConfig)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            lock (_clientsLock)
            {
                if (_clients.Any(c => c.ClientId == client.ClientId))
                {
                    _logger.LogWarning("Client {ClientId} is already registered with the session", client.ClientId);
                    return false;
                }

                // Register event handlers
                client.AudioReceived += OnClientAudioReceived;
                if (client.ClientType == ConversationClientType.Telephony)
                {
                    ((BaseTelephonyConversationClient)client).DTMFReceived += OnClientDTMFReceived;
                }
                client.Disconnected += OnClientDisconnected;

                _clients.Add(client);
            }

            // Create client info in the database
            var clientInfo = new ConversationClientInfo
            {
                ClientId = client.ClientId,
                ClientType = client.ClientType,
                JoinedAt = DateTime.UtcNow,
                AudioInfo = new ConversationMemberAudioInfo()
                { 
                    AudioEncodingType = clientConfig.AudioEncodingType,
                    SampleRate = clientConfig.SampleRate,
                    Channels = clientConfig.Channels,
                    BitsPerSample = clientConfig.BitsPerSample,
                },
                Metadata = new Dictionary<string, string>
                {
                    ["Type"] = client.GetType().Name
                }
            };

            await _conversationStateRepository.AddClientInfoAsync(_sessionId, clientInfo);

            // Notify event subscribers
            ClientAdded?.Invoke(this, new ConversationClientAddedEventArgs(client));

            return true;
        }
        public async Task<bool> RemoveClientAsync(string clientId, string reason)
        {
            IConversationClient? client;

            lock (_clientsLock)
            {
                client = _clients.FirstOrDefault(c => c.ClientId == clientId);
                if (client == null)
                {
                    _logger.LogWarning("Client {ClientId} is not registered with the session", clientId);
                    return false;
                }

                // Unregister event handlers
                client.AudioReceived -= OnClientAudioReceived;
                if (client.ClientType == ConversationClientType.Telephony)
                {
                    ((BaseTelephonyConversationClient)client).DTMFReceived -= OnClientDTMFReceived;
                }
                client.Disconnected -= OnClientDisconnected;

                _clients.Remove(client);
            }

            // Update client info in the database
            await _conversationStateRepository.UpdateClientLeftAsync(_sessionId, clientId, DateTime.UtcNow, reason);

            // Notify event subscribers
            ClientRemoved?.Invoke(this, new ConversationClientRemovedEventArgs(clientId, reason));

            // If there are no more clients, end the session
            // Move this somewhere else, if all clients disconnected, we need better reason handling
            if (_clients.Count == 0)
            {
                await EndAsync(reason + ": All clients disconnected", ConversationSessionEndType.UserEndedCall);
            }

            return true;
        }
        public IReadOnlyList<IConversationClient> GetClients()
        {
            lock (_clientsLock)
            {
                return _clients.ToList().AsReadOnly();
            }
        }
        public IConversationClient? GetTelephonyClientByProviderPhoneNumberId(TelephonyProviderEnum provider, string businessPhoneNumberId)
        {
            lock (_clientsLock)
            {
                return _clients.FirstOrDefault(c => c.ClientType == ConversationClientType.Telephony && ((BaseTelephonyConversationClient)c).ClientId == businessPhoneNumberId && ((BaseTelephonyConversationClient)c).ClientTelephonyProviderType == provider);
            }
        }

        // Agents
        public async Task<FunctionReturnResult> AddPrimaryAgent(IConversationAgent agent)
        {
            var result = new FunctionReturnResult();

            if (_primaryAgent != null)
            {
                return result.SetFailureResult("AddPrimaryAgent:ALREADY_REGISTERED", "A primary agent is already registered with the session");
            }

            var addAgentResult = await AddAgentAsync(agent);
            if (!addAgentResult)
            {
                return result.SetFailureResult("AddPrimaryAgent:ADD_AGENT_FAILED", "Failed to add primary agent");
            }

            _primaryAgent = agent;

            return result.SetSuccessResult();
        }
        public async Task<bool> AddAgentAsync(IConversationAgent agent)
        {
            if (agent == null)
                throw new ArgumentNullException(nameof(agent));

            lock (_agentsLock)
            {
                if (_agents.Any(a => a.AgentId == agent.AgentId))
                {
                    _logger.LogWarning("Agent {AgentId} is already registered with the session", agent.AgentId);
                    return false;
                }

                // Register event handlers
                agent.AudioGenerated += OnAgentAudioGenerated;

                agent.ClearBufferedAudio += OnClearAgentsSentAudioWriteOnClient;

                agent.Thinking += OnAgentThinking;
                agent.ErrorOccurred += OnAgentErrorOccurred;

                _agents.Add(agent);
            }

            // Create agent info in the database
            var agentInfo = new ConversationAgentInfo
            {
                AgentId = agent.AgentId,
                AgentType = agent.AgentType,
                JoinedAt = DateTime.UtcNow,
                AudioInfo = new ConversationMemberAudioInfo()
                {
                    AudioEncodingType = agent.AgentConfiguration.AudioEncodingType,
                    SampleRate = agent.AgentConfiguration.SampleRate,
                    Channels = agent.AgentConfiguration.Channels,
                    BitsPerSample = agent.AgentConfiguration.BitsPerSample,
                },
                Metadata = new Dictionary<string, string>
                {
                    ["Type"] = agent.GetType().Name
                }
            };

            await _conversationStateRepository.AddAgentInfoAsync(_sessionId, agentInfo);

            // Notify event subscribers
            AgentAdded?.Invoke(this, new ConversationAgentAddedEventArgs(agent));

            return true;
        }
        public async Task<bool> RemoveAgentAsync(string agentId, string reason)
        {
            IConversationAgent? agent;

            lock (_agentsLock)
            {
                agent = _agents.FirstOrDefault(a => a.AgentId == agentId);
                if (agent == null)
                {
                    _logger.LogWarning("Agent {AgentId} is not registered with the session", agentId);
                    return false;
                }

                // Unregister event handlers
                agent.AudioGenerated -= OnAgentAudioGenerated;

                agent.ClearBufferedAudio -= OnClearAgentsSentAudioWriteOnClient;

                agent.Thinking -= OnAgentThinking;
                agent.ErrorOccurred -= OnAgentErrorOccurred;

                _agents.Remove(agent);
            }

            // Shut down the agent
            await agent.ShutdownAsync(reason);

            // Update agent info in the database
            await _conversationStateRepository.UpdateAgentLeftAsync(_sessionId, agentId, DateTime.UtcNow, reason);

            // Notify event subscribers
            AgentRemoved?.Invoke(this, new ConversationAgentRemovedEventArgs(agentId, reason));

            // If there are no more agents, end the session
            if (_agents.Count == 0 && _state == ConversationSessionState.Active)
            {
                await EndAsync(reason + ": All agents disconnected", ConversationSessionEndType.AgentEndedCall);
            }

            return true;
        }       
        public IReadOnlyList<IConversationAgent> GetAgents()
        {
            lock (_agentsLock)
            {
                return _agents.ToList().AsReadOnly();
            }
        }

        // Session Management
        public async Task<FunctionReturnResult> NotifyConversationStarted()
        {
            var result = new FunctionReturnResult();

            try
            {
                // Update state
                await UpdateStateAsync(ConversationSessionState.Starting, "Session starting");

                await PrimaryAgent.NotifyConversationStarted().WaitAsync(_sessionCts.Token);

                StartTimers();

                // Update state
                await UpdateStateAsync(ConversationSessionState.Active, "Session active");

                if (IsCallInitiated)
                {
                    if (IsOutboundCall)
                    {
                        _ = _campaignActionExecutorService.SendOutboundConversationSessionAnsweredTelephonyCampaignAction(SessionId);
                    }
                    else if (IsInboundCall)
                    {
                        _ = _campaignActionExecutorService.SendInboundConversationSessionAnsweredTelephonyCampaignAction(SessionId);
                    }
                }
                else if (IsWebInitiated)
                {
                    // TODO
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("NotifyConversationStarted:EXCEPTION", ex.Message);
            }
        }
        public async Task PauseAsync(string reason)
        {
            if (_state != ConversationSessionState.Active)
            {
                _logger.LogWarning("Cannot pause session {SessionId} because it is in state {State}", _sessionId, _state);
                return;
            }

            // Stop timers
            _silenceTimer?.Dispose();
            _silenceTimer = null;

            // Update state
            await UpdateStateAsync(ConversationSessionState.Paused, reason);
        }
        public async Task ResumeAsync()
        {
            if (_state != ConversationSessionState.Paused)
            {
                _logger.LogWarning("Cannot resume session {SessionId} because it is in state {State}", _sessionId, _state);
                return;
            }

            // Restart timers
            StartTimers();

            // Update state
            await UpdateStateAsync(ConversationSessionState.Active, "Session resumed");
        }
        public async Task EndAsync(string reason, ConversationSessionEndType endType, ConversationSessionState finalState = ConversationSessionState.Ended)
        {
            if (_state == ConversationSessionState.Ended || _state == ConversationSessionState.Ending)
            {
                _logger.LogDebug("Session {SessionId} is already ended or ending", _sessionId);
                return;
            }

            var originalState = (ConversationSessionState)((int)_state);

            // Update state
            await UpdateStateAsync(ConversationSessionState.Ending, "Session ending: " + reason);

            // Stop timers
            _silenceTimer?.Dispose();
            _silenceTimer = null;

            _sessionDurationTimer?.Dispose();
            _sessionDurationTimer = null;

            // Dispose cancellation token source
            _sessionCts?.Cancel();
            _sessionCts?.Dispose();
            _sessionCts = null;

            // Disconnect all clients
            foreach (var client in GetClients())
            {
                try
                {
                    await client.DisconnectAsync(reason);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disconnecting client {ClientId}", client.ClientId);
                }
            }

            // Shutdown all agents
            foreach (var agent in GetAgents())
            {
                try
                {
                    await agent.ShutdownAsync(reason);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error shutting down agent {AgentId}", agent.AgentId);
                }
            }

            // Update state
            await _conversationStateRepository.UpdateEndType(_sessionId, endType);
            await UpdateStateAsync(finalState, reason);

            // Only Charge Active States
            if (originalState == ConversationSessionState.Active)
            {
                decimal? durationSeconds = await UpdateFinalMetricsAsync();
                if (durationSeconds == null)
                {
                    _logger.LogError("Failed to update final metrics for session {SessionId}", _sessionId);
                    durationSeconds = 0;
                }

                List<ConsumedFeatureInput> consumedFeatureInputs = new List<ConsumedFeatureInput>();
                // Call Minutes Used
                consumedFeatureInputs.Add(
                    new ConsumedFeatureInput(
                        BillingFeatureKey.CallMinutes,
                        (decimal)(durationSeconds.Value / 60m)
                    )
                );
                // Voicemail Detection
                var hasVoiceMailDetection = false;
                if (IsOutboundCall)
                {
                    if (_sessionCallQueueTelephonyCampaignData!.VoicemailDetection.IsEnabled)
                    {
                        consumedFeatureInputs.Add(
                            new ConsumedFeatureInput(
                                BillingFeatureKey.VoicemailDetection,
                                1
                            )
                        );
                    }
                }   

                await _userBillingUsageManager.ProcessAndBillUsageAsync(_sessionBusinessData.MasterUserEmail, _sessionBusinessData.Id, consumedFeatureInputs, UserUsageSourceTypeEnum.Conversation, _sessionId, "Conversation Session Usage");

                // Run Audio Compilation in the background
                RunAudioCompilationAsync();
            }  

            try
            {
                if (IsCallInitiated)
                {
                    if (IsOutboundCall)
                    {
                        _ = _campaignActionExecutorService.SendOutboundConversationSessionEndedTelephonyCampaignAction(SessionId, reason);
                    }
                    else if (IsInboundCall)
                    {
                        _ = _campaignActionExecutorService.SendInboundConversationSessionEndedTelephonyCampaignAction(SessionId);
                    }
                }
                else if (IsWebInitiated)
                {
                    _ = _campaignActionExecutorService.SendWebConversationSessionCampaignAction(SessionId);
                }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking session actions for session {SessionId}", SessionId);
            }

            // On SessionEnded Cleanup for Parent Manager
            if (SessionEnded != null)
            {
                try
                {
                    await SessionEnded.Invoke(this);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error invoking SessionEnded event handler for session {SessionId}", SessionId);
                }
            }

            // Dispose
            Dispose();
        }
        public async Task UpdateStateAsync(ConversationSessionState newState, string reason)
        {
            var oldState = _state;
            _state = newState;

            // Update the state in the repository
            await _conversationStateRepository.UpdateStatusAsync(_sessionId, newState);

            // Add a log entry
            _logger.LogDebug($"State changed from {oldState} to {newState}: {reason}");

            // Notify event subscribers
            StateChanged?.Invoke(this, new ConversationSessionStateChangedEventArgs(oldState, newState, reason));
        }
        public async Task<IReadOnlyList<ConversationTurn>> GetTurnsAsync()
        {
            var state = await _conversationStateRepository.GetByIdAsync(_sessionId);
            return state?.Turns?.AsReadOnly() ?? new List<ConversationTurn>().AsReadOnly();
        }
        public async Task<ConversationTurn?> GetTurnAsync(string turnId)
        {
            var state = await _conversationStateRepository.GetByIdAsync(_sessionId);
            return state?.Turns?.FirstOrDefault(t => t.Id == turnId);
        }

        // Timers for Silence / Max Duration
        private void StartTimers()
        {
            // Start silence detection timer
            _silenceTimer = new Timer(CheckSilence, null, _sessionContextData.Timeout.NotifyOnSilenceMS, _sessionContextData.Timeout.NotifyOnSilenceMS);

            // Start session duration timer
            var maxDurationMs = _sessionContextData.Timeout.MaxCallTimeS * 1000;
            _sessionDurationTimer = new Timer(async (state) => { await EndSessionOnMaxDuration(state); }, null, maxDurationMs, Timeout.Infinite);
        }
        private void CheckSilence(object? state)
        {
            var silenceDuration = DateTime.UtcNow - _lastUserActivityTime;

            if (silenceDuration.TotalMilliseconds > _sessionContextData.Timeout.EndCallOnSilenceMS)
            {
                EndAsync("Silence timeout reached", ConversationSessionEndType.UserSilenceTimeoutReached).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        _logger.LogError(t.Exception, "Error ending session due to silence");
                    }
                });
            }
            else if (silenceDuration.TotalMilliseconds > _sessionContextData.Timeout.NotifyOnSilenceMS)
            {
                lock (_agentsLock)
                {
                    foreach (var agent in _agents)
                    {
                        // TODO disabled for now
                        // todo get these texts from user what to say
                        //agent.ProcessTextAsync($"<silence duration=\"{silenceDuration.TotalSeconds:F1}s\">", null, CancellationToken.None)
                        //    .ContinueWith(t =>
                        //    {
                        //        if (t.IsFaulted)
                        //        {
                        //            _logger.LogError(t.Exception, "Error notifying agent about silence");
                        //        }
                        //    });
                    }
                }
            }
        }
        private async Task EndSessionOnMaxDuration(object? state)
        {
            _logger.LogDebug("Ending session {SessionId} due to max duration", _sessionId);

            bool isAnyAIAgentToEndCall = false;
            foreach (var agent in _agents)
            {
                if (agent.GetType() == typeof(ConversationAIAgent))
                {
                    isAnyAIAgentToEndCall = true;
                }

                _ = agent.NotifyMaxDurationReached();
            }

            if (isAnyAIAgentToEndCall)
            {
                await Task.Delay(5000, _sessionCts.Token);
                // if session still not ended after 5 seconds, force end it

                if (_sessionCts.IsCancellationRequested)
                {
                    return;
                }
            }

            // todo play a default message to end the call

            await EndAsync("Maximum session duration reached", ConversationSessionEndType.MaxConversationDurationReached).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    _logger.LogError(t.Exception, "Error ending session due to max duration");
                }
            });
        }

        // Ending Methods
        private async Task<decimal?> UpdateFinalMetricsAsync()
        {
            try
            {
                var state = await _conversationStateRepository.GetByIdAsync(_sessionId);
                if (state == null) return null;

                var metrics = state.Metrics;

                // Calculate duration
                if (state.EndTime.HasValue && state.StartTime != default)
                {
                    metrics.DurationSeconds = (state.EndTime.Value - state.StartTime).TotalSeconds;
                }

                // Update metrics in the repository
                await _conversationStateRepository.UpdateMetricsAsync(_sessionId, metrics);

                return (decimal)metrics.DurationSeconds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating final metrics for session {SessionId}", _sessionId);
                return null;
            }
        }
        private void RunAudioCompilationAsync()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var compileService = new SessionAudioCompilationService(_sessionLoggerFactory.CreateLogger<SessionAudioCompilationService>(), _conversationStateRepository, _audioStorageManager);
                    await compileService.CompileConversationAudioAsync(_sessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing audio compilation background task for session {SessionId}", _sessionId);
                }
            });
        }

        // Turn Event Handlers
        public async Task NotifyTurnStarted(ConversationTurn turn)
        {
            await _conversationStateRepository.StartNewTurnAsync(_sessionId, turn);
            TurnStarted?.Invoke(this, new ConversationTurnEventArgs(turn));
        }
        public async Task NotifyTurnUpdated(ConversationTurn turn)
        {
            await _conversationStateRepository.UpdateTurnAsync(_sessionId, turn);
            TurnUpdated?.Invoke(this, new ConversationTurnEventArgs(turn));
            if (turn.Status == ConversationTurnStatus.Completed || turn.Status == ConversationTurnStatus.Interrupted || turn.Status == ConversationTurnStatus.Error)
            {
                TurnCompleted?.Invoke(this, new ConversationTurnEventArgs(turn));
            }
        }

        // Client Event Handlers
        private void OnClearAgentsSentAudioWriteOnClient(object? sender, object? data)
        {
            if (sender is string)
            {
                sender = _agents.Find(c => c.AgentId == (string)sender);
            }

            if (sender is not IConversationAgent agent)
                return;

            foreach (var client in _clients)
            {
                if (client is TwilioConversationClient twilioClient)
                {
                    twilioClient.ClearBufferedAudioAync(CancellationToken.None).GetAwaiter().GetResult();
                }
                else if (client is ModemTelConversationClient modemClient)
                {
                    modemClient.ClearBufferedAudioAync(CancellationToken.None).GetAwaiter().GetResult();
                }
            }
        }
        private void OnClientAudioReceived(object? sender, ConversationAudioReceivedEventArgs e)
        {
            if (_state == ConversationSessionState.Ending || _state == ConversationSessionState.Ended) return;

            if (sender is string)
            {
                sender = _clients.Find(c => c.ClientId == (string)sender);
            }

            if (sender is not IConversationClient client)
                return;

            // Update last activity time for silence detection
            _lastUserActivityTime = DateTime.UtcNow;

            // Store audio if recording is enabled
            if (_sessionContextData.RecordCallAudio)
            {
                try
                {
                    string audioReference = $"{_sessionId}/{client.ClientId}/{Guid.NewGuid()}";
                    _ = _audioStorageManager.StoreAudioAsync(audioReference, e.AudioData);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error storing audio from client {ClientId}", client.ClientId);
                }
            }

            // If target agent is specified, send only to that agent
            if (!string.IsNullOrEmpty(e.TargetAgentId))
            {
                var agent = GetAgents().FirstOrDefault(a => a.AgentId == e.TargetAgentId);
                if (agent != null)
                {
                    agent.ProcessAudioAsync(e.AudioData, client.ClientId, _sessionCts.Token);
                }
                else
                {
                    _logger.LogWarning("Target agent {TargetAgentId} not found", e.TargetAgentId);
                }
            }

            // Otherwise Forward the audio to all agents
            foreach (var agent in GetAgents())
            {
                try
                {
                    agent.ProcessAudioAsync(e.AudioData, client.ClientId, _sessionCts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing audio for agent {AgentId}", agent.AgentId);
                }
            }
        }
        private async void OnClientDTMFReceived(object? sender, ConversationDTMFReceivedEventArgs e)
        {
            if (_state == ConversationSessionState.Ending || _state == ConversationSessionState.Ended) return;

            if (sender is string)
            {
                sender = _clients.Find(c => c.ClientId == (string)sender);
            }

            if (sender is not IConversationClient client)
                return;

            // Update last activity time for silence detection
            _lastUserActivityTime = DateTime.UtcNow;

            // Notify event subscribers
            DTMFRecieved?.Invoke(this, new ConversationDTMFReceivedEventArgs(client.ClientId, e.Digit));

            // if target agent is specified, send only to that agent
            if (!string.IsNullOrEmpty(e.TargetAgentId))
            {
                var agent = GetAgents().FirstOrDefault(a => a.AgentId == e.TargetAgentId);
                if (agent != null)
                {
                    await agent.ProcessDTMFAsync(e.Digit, client.ClientId, CancellationToken.None);
                }
                else
                {
                    _logger.LogWarning("Target agent {TargetAgentId} not found", e.TargetAgentId);
                }
                return;
            }

            // Otherwise Forward the text to all agents
            foreach (var agent in GetAgents())
            {
                try
                {
                    await agent.ProcessDTMFAsync(e.Digit, client.ClientId, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending digit to agent {AgentId}", agent.AgentId);
                }
            }
        }
        private async void OnClientDisconnected(object? sender, ConversationClientDisconnectedEventArgs e)
        {
            if (sender is string)
            {
                sender = _clients.Find(c => c.ClientId == (string)sender);
            }

            if (sender is not IConversationClient client)
                return;

            // Remove the client
            await RemoveClientAsync(client.ClientId, e.Reason);
        }

        // Agent Event Handlers
        private async void OnAgentAudioGenerated(object? sender, ConversationAudioGeneratedEventArgs e)
        {
            if (_state == ConversationSessionState.Ending || _state == ConversationSessionState.Ended) return;

            if (sender is string)
            {
                sender = _agents.Find(a => a.AgentId == (string)sender);
            }

            if (sender is not IConversationAgent agent)
                return;

            // Store audio if recording is enabled
            if (_sessionContextData.RecordCallAudio)
            {
                try
                {
                    string audioReference = $"{_sessionId}/{agent.AgentId}/{Guid.NewGuid()}";
                    _ = _audioStorageManager.StoreAudioAsync(audioReference, e.AudioData);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error storing audio from agent {AgentId}", agent.AgentId);
                }
            }

            // If target client is specified, send only to that client
            if (!string.IsNullOrEmpty(e.TargetClientId))
            {
                var targetClient = GetClients().FirstOrDefault(c => c.ClientId == e.TargetClientId);
                if (targetClient != null)
                {
                    _ = targetClient.SendAudioAsync(e.AudioData, CancellationToken.None);
                }
                else
                {
                    _logger.LogWarning("Target client {ClientId} not found for audio from agent {AgentId}", e.TargetClientId, agent.AgentId);
                }
                return;
            }

            // Otherwise, broadcast to all clients
            foreach (var client in GetClients())
            {
                try
                {
                    _ = client.SendAudioAsync(e.AudioData, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending audio to client {ClientId}", client.ClientId);
                }
            }
        }
        
        private void OnAgentThinking(object? sender, ConversationAgentThinkingEventArgs e)
        {
            if (sender is string)
            {
                sender = _agents.Find(a => a.AgentId == (string)sender);
            }

            if (sender is not IConversationAgent agent)
                return;
        }
        private void OnAgentErrorOccurred(object? sender, ConversationAgentErrorEventArgs e)
        {
            if (sender is string)
            {
                sender = _agents.Find(a => a.AgentId == (string)sender);
            }

            if (sender is not IConversationAgent agent)
                return;

            _logger.LogError(e.Exception, "Agent {AgentId} error: {ErrorMessage}", agent.AgentId, e.ErrorMessage);

            // End the session if it's a critical error
            if (e.Severity == ConversationErrorSeverity.Critical)
            {
                EndAsync($"Critical agent error: {e.ErrorMessage}", ConversationSessionEndType.MidSessionFailure).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        _logger.LogError(t.Exception, "Error ending session due to critical agent error");
                    }
                });
            }
        }

        // Disposal
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                disposedValue = true;
            }
        }
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}