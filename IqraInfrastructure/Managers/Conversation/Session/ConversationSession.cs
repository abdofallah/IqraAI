using Google.Protobuf.WellKnownTypes;
using IqraCore.Entities.Business;
using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Conversation;
using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Conversation.Context;
using IqraCore.Entities.Conversation.Context.Action;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Helper.Agent;
using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.Conversation;
using IqraInfrastructure.Managers.Billing;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Conversation.Session.Agent.AI;
using IqraInfrastructure.Managers.Conversation.Session.Agent.AI.Helpers;
using IqraInfrastructure.Managers.Conversation.Session.Client;
using IqraInfrastructure.Managers.Conversation.Session.Client.Telephony;
using IqraInfrastructure.Managers.Conversation.Session.Helpers;
using IqraInfrastructure.Repositories.Call;
using IqraInfrastructure.Repositories.Conversation;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Web;

namespace IqraInfrastructure.Managers.Conversation.Session
{
    public class ConversationSession : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ConversationSession> _logger;
        private readonly BusinessManager _businessManager;
        private readonly OutboundCallCampaignRepository _outboundCallCampaignRepository;
        private readonly ConversationStateRepository _conversationStateRepository;
        private readonly ConversationAudioRepository _audioStorageManager;
        private readonly BillingUsageManager _billingProcessingManager;

        private readonly string _sessionId;
        private readonly DateTime _createdAt;
        private CallQueueData _sessionCallQueueData;
        private readonly string _callOrWebInitiated;

        private readonly List<IConversationClient> _clients = new();
        private IConversationClient? _primaryClient = null;

        private readonly List<IConversationAgent> _agents = new();
        private IConversationAgent? _primaryAgent = null;

        private readonly List<ConversationMessage> _messages = new();

        private BusinessData _sessionBusinessData;
        private BusinessApp _sessionBusinessAppData;
        private ConversationSessionContext _sessionContextData;

        private readonly object _clientsLock = new();
        private readonly object _agentsLock = new();
        private readonly object _messagesLock = new();

        private ConversationSessionState _state = ConversationSessionState.Created;
        private DateTime _lastUserActivityTime = DateTime.UtcNow;
        private Timer? _silenceTimer;
        private Timer? _sessionDurationTimer;

        private CancellationTokenSource _sessionCts;
        private bool disposedValue;

        public event EventHandler<ConversationSessionStateChangedEventArgs>? StateChanged;
        public event EventHandler<ConversationMessageAddedEventArgs>? MessageAdded;
        public event EventHandler<ConversationDTMFReceivedEventArgs>? DTMFRecieved;
        public event EventHandler<ConversationClientAddedEventArgs>? ClientAdded;
        public event EventHandler<ConversationClientRemovedEventArgs>? ClientRemoved;
        public event EventHandler<ConversationAgentAddedEventArgs>? AgentAdded;
        public event EventHandler<ConversationAgentRemovedEventArgs>? AgentRemoved;

        public event EventHandler<object>? SessionEnded;

        public string SessionId => _sessionId;
        public ConversationSessionState State => _state;
        public bool IsCallInitiated => _callOrWebInitiated == "call";
        public bool IsWebInitiated => _callOrWebInitiated == "web";
        public IConversationClient? PrimaryClient => _primaryClient;
        public IConversationAgent? PrimaryAgent => _primaryAgent;

        public ConversationSession(
            string sessionId,
            CallQueueData queueData,
            string callOrWebInitiated,
            CancellationTokenSource sessionCTS,

            BusinessManager businessManager,
            OutboundCallCampaignRepository outboundCallCampaignRepository,
            ConversationStateRepository conversationStateRepository,
            ConversationAudioRepository audioStorageManager,
            BillingUsageManager billingProcessingManager,
            ILoggerFactory loggerFactory
        )
        {
            _sessionId = sessionId;
            _createdAt = DateTime.UtcNow;
            _sessionCallQueueData = queueData;
            _callOrWebInitiated = callOrWebInitiated;
            _sessionCts = sessionCTS;

            _businessManager = businessManager;
            _outboundCallCampaignRepository = outboundCallCampaignRepository;
            _conversationStateRepository = conversationStateRepository;
            _audioStorageManager = audioStorageManager;
            _billingProcessingManager = billingProcessingManager;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<ConversationSession>();
        }

        public async Task<FunctionReturnResult> InitalizeAsync()
        {
            var result = new FunctionReturnResult();

            try
            {
                await InitalizeConversationConfigurationAsync();
                await InitializeConversationStateAsync();

                return result.SetSuccessResult();
            }
            catch (Exception ex) {
                return result.SetFailureResult("InitalizeAsync:EXCEPTION", $"Failed to initialize conversation session state: {ex.Message}");
            }
        }

        public async Task InitalizeConversationConfigurationAsync()
        {
            var businessData = await _businessManager.GetUserBusinessById(_sessionCallQueueData.BusinessId, "InitalizeConversationConfigurationAsync");
            if (!businessData.Success)
            {
                _logger.LogError("Business data not found for business ID {BusinessId}", _sessionCallQueueData.BusinessId);
                throw new InvalidOperationException($"Business data not found for business ID {_sessionCallQueueData.BusinessId}");
            }
            _sessionBusinessData = businessData.Data;

            var businessAppData = await _businessManager.GetUserBusinessAppById(_sessionCallQueueData.BusinessId, "InitalizeConversationConfigurationAsync");
            if (!businessAppData.Success)
            {
                _logger.LogError("Business app data not found for business ID {BusinessId}", _sessionCallQueueData.BusinessId);
                throw new InvalidOperationException($"Business app data not found for business ID {_sessionCallQueueData.BusinessId}");
            }
            _sessionBusinessAppData = businessAppData.Data;

            if (_sessionCallQueueData.Type == CallQueueTypeEnum.Inbound)
            {
                InboundCallQueueData inboundCallQueue = _sessionCallQueueData as InboundCallQueueData;

                var businessRouteData = businessAppData.Data.Routings.Find(r => r.Id == inboundCallQueue.RouteId);
                if (businessRouteData == null)
                {
                    _logger.LogError("Business route data not found for business ID {BusinessId} and route ID {RouteId}", _sessionCallQueueData.BusinessId, inboundCallQueue.RouteId);
                    throw new InvalidOperationException($"Business route data not found for business ID {_sessionCallQueueData.BusinessId} and route ID {inboundCallQueue.RouteId}");
                }

                _sessionContextData = new ConversationSessionContext()
                {
                    Agent = new ConversationSessionContextAgent()
                    {
                        SelectedAgentId = businessRouteData.Agent.SelectedAgentId,
                        OpeningScriptId = businessRouteData.Agent.OpeningScriptId,
                        Interruption = businessRouteData.Agent.Interruption,
                        TelephonyNumberInContext = businessRouteData.Agent.RouteNumberInContext,
                        CallerNumberInContext = businessRouteData.Agent.CallerNumberInContext,
                        Timezones = businessRouteData.Agent.Timezones
                    },
                    Timeout = new ConversationSessionContextTimeout()
                    {
                        PickUpDelayMS = businessRouteData.Configuration.PickUpDelayMS,
                        NotifyOnSilenceMS = businessRouteData.Configuration.NotifyOnSilenceMS,
                        EndCallOnSilenceMS = businessRouteData.Configuration.EndCallOnSilenceMS,
                        MaxCallTimeS = businessRouteData.Configuration.MaxCallTimeS,
                        RecordCallAudio = businessRouteData.Configuration.RecordCallAudio
                    },
                    Language = new ConversationSessionContextLanguage()
                    {
                        DefaultLanguageCode = businessRouteData.Language.DefaultLanguageCode,
                        MultiLanguageEnabled = businessRouteData.Language.MultiLanguageEnabled,
                        EnabledMultiLanguages = businessRouteData.Language.EnabledMultiLanguages
                    },
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

                var campaignData = await _outboundCallCampaignRepository.GetOutboundCallCampaignByIdAsync(outboundCallQueue.CampaignId);
                if (campaignData == null)
                {
                    _logger.LogError("Outbound call campaign data not found for business ID {BusinessId} and queue ID {RouteId}", _sessionCallQueueData.BusinessId, outboundCallQueue.Id);
                    throw new InvalidOperationException($"Outbound call campaign not found for business ID {_sessionCallQueueData.BusinessId} and queue ID {outboundCallQueue.Id}");
                }

                _sessionContextData = new ConversationSessionContext()
                {
                    Agent = new ConversationSessionContextAgent()
                    {
                        SelectedAgentId = outboundCallQueue.AgentId,
                        OpeningScriptId = outboundCallQueue.AgentScriptId,
                        Timezones = outboundCallQueue.AgentTimeZone,
                        TelephonyNumberInContext = campaignData.CallRequestData.AgentSettings.IncludeFromNumberInContext.Value,
                        RecipientNumberInContext = campaignData.CallRequestData.AgentSettings.IncludeToNumberInContext.Value,
                    },
                    Language = new ConversationSessionContextLanguage()
                    {
                        DefaultLanguageCode = outboundCallQueue.AgentLanguageCode,
                        MultiLanguageEnabled = false
                    },
                    Timeout = new ConversationSessionContextTimeout()
                    {
                        NotifyOnSilenceMS = campaignData.CallRequestData.Configuration.Timeouts.NotifyOnSilenceMS.Value,
                        EndCallOnSilenceMS = campaignData.CallRequestData.Configuration.Timeouts.EndOnSilenceMS.Value,
                        MaxCallTimeS = campaignData.CallRequestData.Configuration.Timeouts.MaxCallTimeS.Value,
                    },
                    DynamicVariables = campaignData.CallRequestData.DynamicVariables,
                    Metadata = campaignData.CallRequestData.Metadata
                };

                // ACTIONS
                if (campaignData.CallRequestData.Actions != null)
                {
                    // Ended
                    if (campaignData.CallRequestData.Actions.Ended != null && campaignData.CallRequestData.Actions.Ended.ToolId != null)
                    {
                        _sessionContextData.CallEndedAction = new ConversationSessionContextAction()
                        {
                            SelectedToolId = campaignData.CallRequestData.Actions.Ended.ToolId,
                            Arguments = campaignData.CallRequestData.Actions.Ended.Arguments ?? new Dictionary<string, object>()
                        };
                    }
                }

                // INTERRUPTION
                if (campaignData.CallRequestData.AgentSettings.Interruption.Type == AgentInterruptionTypeENUM.TurnByTurn)
                {
                    _sessionContextData.Agent.Interruption = new BusinessAppRouteAgentInterruptionTurnByTurn()
                    {
                        UseInterruptedResponseInNextTurn = campaignData.CallRequestData.AgentSettings.Interruption.UseInterruptedResponseInNextTurn.Value
                    };
                }
                else if (campaignData.CallRequestData.AgentSettings.Interruption.Type == AgentInterruptionTypeENUM.InterruptibleViaVAD)
                {
                    _sessionContextData.Agent.Interruption = new BusinessAppRouteAgentInterruptionViaVAD()
                    {
                        InterruptibleConversationAudioActivityDurationMS = campaignData.CallRequestData.AgentSettings.Interruption.VadDurationMS.Value
                    };
                }
                else if (campaignData.CallRequestData.AgentSettings.Interruption.Type == AgentInterruptionTypeENUM.InterruptibleViaAI)
                {
                    var interruptionVIAAI = new BusinessAppRouteAgentInterruptionViaAI()
                    {
                        UseCurrentAgentLLMForInterrupting = campaignData.CallRequestData.AgentSettings.Interruption.UseAgentLLM.Value
                    };
                    if (campaignData.CallRequestData.AgentSettings.Interruption.UseAgentLLM.Value == true)
                    {
                        interruptionVIAAI.LLMIntegrationToUseForCheckingInterruption = new BusinessAppAgentIntegrationData()
                        {
                            Id = campaignData.CallRequestData.AgentSettings.Interruption.LLMIntegrationId,
                            FieldValues = campaignData.CallRequestData.AgentSettings.Interruption.LLMIntegrationConfigFields
                        };
                    }

                    _sessionContextData.Agent.Interruption = interruptionVIAAI;
                }
                else if (campaignData.CallRequestData.AgentSettings.Interruption.Type == AgentInterruptionTypeENUM.InterruptibleViaResponse)
                {
                    _sessionContextData.Agent.Interruption = new BusinessAppRouteAgentInterruptionViaResponse();
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported interruption type: {campaignData.CallRequestData.AgentSettings.Interruption.Type}");
                }
            }
        }

        public async Task InitializeConversationStateAsync()
        {
            var conversationState = new ConversationState
            {
                Id = _sessionId,
                BusinessMasterEmail = _sessionBusinessData.MasterUserEmail,
                BusinessId = _sessionCallQueueData.BusinessId,
                QueueId = _sessionCallQueueData.Id,
                Status = ConversationSessionState.Created,
                StartTime = DateTime.UtcNow,
                LastActivityTime = DateTime.UtcNow,
                ProcessingServerId = _sessionCallQueueData.ProcessingBackendServerId,
                RegionId = _sessionCallQueueData.RegionId,
                ExpectedEndTimeAt = DateTime.UtcNow.AddSeconds(_sessionContextData.Timeout.MaxCallTimeS)
            };

            await _conversationStateRepository.CreateAsync(conversationState);
        }

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
                client.TextReceived += OnClientTextReceived;
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

            _logger.LogInformation("Added client {ClientId} to session {SessionId}", client.ClientId, _sessionId);
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
                client.TextReceived -= OnClientTextReceived;
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
            if (_clients.Count == 0)
            {
                await EndAsync(reason + ": All clients disconnected");
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

                agent.AgentTextResponse += OnAgentTextResponse;
                agent.ClientTextQuery += OnClientTextReceived;
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

            _logger.LogInformation("Added agent {AgentId} to session {SessionId}", agent.AgentId, _sessionId);
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

                agent.AgentTextResponse -= OnAgentTextResponse;
                agent.ClientTextQuery -= OnClientTextReceived;
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
                await EndAsync(reason + ": All agents disconnected");
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

        public async Task<FunctionReturnResult> StartAsync()
        {
            var result = new FunctionReturnResult();

            try
            {
                // Update state
                await UpdateStateAsync(ConversationSessionState.Starting, "Session starting");

                await PrimaryAgent.InitializeAsync(_sessionBusinessAppData, _sessionContextData, _sessionCts.Token);

                // Start silence and max duration detection timer
                StartTimers();

                // Update state
                await UpdateStateAsync(ConversationSessionState.Active, "Session active");          

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("StartAsync:EXCEPTION", ex.Message);
            }
        }

        public async Task<FunctionReturnResult> NotifyConversationStarted()
        {
            var result = new FunctionReturnResult();

            try
            {
                await PrimaryAgent.NotifyConversationStarted().WaitAsync(_sessionCts.Token);

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("NotifyConversationStarted:EXCEPTION", ex.Message);
            }
        }

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
                EndAsync("Silence timeout reached").ContinueWith(t =>
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
            _logger.LogInformation("Ending session {SessionId} due to max duration", _sessionId);

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

            await EndAsync("Maximum session duration reached").ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    _logger.LogError(t.Exception, "Error ending session due to max duration");
                }
            });
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

            _logger.LogInformation("Session {SessionId} paused: {Reason}", _sessionId, reason);
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

            _logger.LogInformation("Session {SessionId} resumed", _sessionId);
        }

        public async Task EndAsync(string reason, ConversationSessionState finalState = ConversationSessionState.Ended)
        {
            if (_state == ConversationSessionState.Ended || _state == ConversationSessionState.Ending)
            {
                _logger.LogDebug("Session {SessionId} is already ended", _sessionId);
                return;
            }

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
            await UpdateStateAsync(finalState, reason);

            // Calculate final metrics
            double? durationSeconds = await UpdateFinalMetricsAsync();
            if (durationSeconds == null)
            {
                _logger.LogError("Failed to update final metrics for session {SessionId}", _sessionId);
                durationSeconds = 0;
            }

            // Add Minutes Usage to the Account if atleast 5 seconds of call
            if (durationSeconds >= 5)
            {
                await _billingProcessingManager.ProcessAndBillUsageAsync(_sessionId, _sessionBusinessData.Id, _sessionBusinessData.MasterUserEmail, durationSeconds.Value);
            }

            _ = Task.Run(async () =>
            {
                await ExecuteEndCallAction();
            });

            // Run Audio Compilation in the background
            RunAudioCompilationAsync();

            SessionEnded?.Invoke(this, null);

            // Dispose
            Dispose();
        }

        private async Task<double?> UpdateFinalMetricsAsync()
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

                return metrics.DurationSeconds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating final metrics for session {SessionId}", _sessionId);
                return null;
            }
        }
        private void RunAudioCompilationAsync()
        {
            _logger.LogInformation("Queueing audio compilation background task for session {SessionId}", _sessionId);
            _ = Task.Run(async () =>
            {
                try
                {
                    var compileService = new SessionAudioCompilationService(_loggerFactory.CreateLogger<SessionAudioCompilationService>(), _conversationStateRepository, _audioStorageManager);
                    await compileService.CompileConversationAudioAsync(_sessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing audio compilation background task for session {SessionId}", _sessionId);
                }
            });
        }

        private async Task ExecuteEndCallAction()
        {
            if (_sessionContextData.CallEndedAction.SelectedToolId == null)
            {
                return;
            }

            BusinessAppTool? endCallTool = _sessionBusinessAppData.Tools.Find(t => t.Id == _sessionContextData.CallEndedAction.SelectedToolId);
            if (endCallTool == null)
            {
                AddLogEntry(ConversationLogLevel.Error, "Call ended action failed", "Tool not found");
                return;
            }

            var parsedArguments = new Dictionary<string, object?>();

            string? cachedConversation = null;
            string? cachedMetadata = null;

            foreach (var argument in _sessionContextData.CallEndedAction.Arguments)
            {
                var argumentName = argument.Key;
                var argumentValue = argument.Value;

                if (argument.Value is string stringValue)
                {
                    if (stringValue.Contains("{{conversation}}") || stringValue.Contains("{={conversation}=}"))
                    {
                        if (cachedConversation == null)
                        {
                            cachedConversation = JsonSerializer.Serialize(_messages);
                        }

                        stringValue = stringValue
                            .Replace("{{conversation}}", cachedConversation)
                            .Replace("{={conversation}=}", cachedConversation);
                    }

                    if (stringValue.Contains("{{metadata}}") || stringValue.Contains("{={metadata}=}"))
                    {
                        if (cachedMetadata == null)
                        {
                            cachedMetadata = JsonSerializer.Serialize(_sessionContextData.Metadata);
                        }

                        stringValue = stringValue
                            .Replace("{{metadata}}", cachedMetadata)
                            .Replace("{={metadata}=}", cachedMetadata);
                    }

                    if (_sessionCallQueueData.Type == CallQueueTypeEnum.Inbound)
                    {
                        var callerNumber = (_sessionCallQueueData as InboundCallQueueData).CallerNumber;

                        stringValue = stringValue
                            .Replace("{{from_number}}", callerNumber)
                            .Replace("{={from_number}=}", callerNumber);
                    }
                    else if (_sessionCallQueueData.Type == CallQueueTypeEnum.Outbound)
                    {
                        var recipientNumber = (_sessionCallQueueData as OutboundCallQueueData).RecipientNumber;

                        stringValue = stringValue
                            .Replace("{{to_number}}", recipientNumber)
                            .Replace("{={to_number}=}", recipientNumber);
                    }

                    stringValue = stringValue
                        .Replace("{{call_answered}}", _createdAt.ToString())
                        .Replace("{={call_answered}=}", _createdAt.ToString());

                    parsedArguments[argumentName] = stringValue;
                }
                else
                {
                    parsedArguments[argumentName] = argument.Value;
                }
            }

            CustomToolExecutionHelper endCallToolhelper = new CustomToolExecutionHelper(_loggerFactory);
            endCallToolhelper.Initialize(_sessionBusinessAppData, _sessionContextData.Language.DefaultLanguageCode);

            var endToolResult = await endCallToolhelper.ExecuteHttpRequestForToolAsync(
                endCallTool,
                parsedArguments,
                CancellationToken.None
            );
            if (!endToolResult.Success)
            {
                AddLogEntry(ConversationLogLevel.Error, "Call ended action failed", $"{endToolResult.Code}: {endToolResult.Message}");
                return;
            }

            AddLogEntry(ConversationLogLevel.Information, "Call ended action executed successfully");
        }

        public IReadOnlyList<ConversationMessage> GetHistory()
        {
            lock (_messagesLock)
            {
                return _messages.ToList().AsReadOnly();
            }
        }

        public void AddLogEntry(ConversationLogLevel level, string message, object? data = null)
        {
            try
            {
                string? dataJson = null;
                if (data != null)
                {
                    dataJson = System.Text.Json.JsonSerializer.Serialize(data);
                }

                var logEntry = new ConversationLogEntry
                {
                    Level = level,
                    Message = message,
                    Timestamp = DateTime.UtcNow,
                    DataJson = dataJson
                };

                _conversationStateRepository.AddLogEntryAsync(_sessionId, logEntry)
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            _logger.LogError(t.Exception, "Error adding log entry to conversation {SessionId}", _sessionId);
                        }
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating log entry");
            }
        }

        public async Task UpdateStateAsync(ConversationSessionState newState, string reason)
        {
            var oldState = _state;
            _state = newState;

            // Update the state in the repository
            await _conversationStateRepository.UpdateStatusAsync(_sessionId, newState);

            // Add a log entry
            AddLogEntry(ConversationLogLevel.Information, $"State changed from {oldState} to {newState}: {reason}");

            // Notify event subscribers
            StateChanged?.Invoke(this, new ConversationSessionStateChangedEventArgs(oldState, newState, reason));
        }

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
            if (sender is string)
            {
                sender = _clients.Find(c => c.ClientId == (string)sender);
            }

            if (sender is not IConversationClient client)
                return;

            // Update last activity time for silence detection
            _lastUserActivityTime = DateTime.UtcNow;

            // Store audio if recording is enabled
            if (_sessionContextData.Timeout.RecordCallAudio)
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
                    agent.ProcessAudioAsync(e.AudioData, client.ClientId, CancellationToken.None);
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
                    agent.ProcessAudioAsync(e.AudioData, client.ClientId, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing audio for agent {AgentId}", agent.AgentId);
                }
            }
        }
        private async void OnClientTextReceived(object? sender, ConversationTextReceivedEventArgs e)
        {
            if (sender is string)
            {
                sender = _clients.Find(c => c.ClientId == (string)sender);
            }

            if (sender is not IConversationClient client)
                return;

            _logger.LogInformation("Received text from client {ClientId}: {Text}", client.ClientId, e.Text);

            // Update last activity time for silence detection
            _lastUserActivityTime = DateTime.UtcNow;

            // Create a message
            var message = new ConversationMessage(client.ClientId, ConversationSenderRole.Client, e.Text);

            // Add to history
            lock (_messagesLock)
            {
                _messages.Add(message);
            }

            // Create message data for storage
            var messageData = new ConversationMessageData
            {
                SenderId = client.ClientId,
                Role = ConversationSenderRole.Client,
                Content = e.Text,
                Timestamp = DateTime.UtcNow
            };

            // Save to repository
            await _conversationStateRepository.AddMessageAsync(_sessionId, messageData);

            // Notify event subscribers
            MessageAdded?.Invoke(this, new ConversationMessageAddedEventArgs(message));

            if (e.OnlySave) return;

            // if target agent is specified, send only to that agent
            if (!string.IsNullOrEmpty(e.TargetAgentId))
            {
                var agent = GetAgents().FirstOrDefault(a => a.AgentId == e.TargetAgentId);
                if (agent != null)
                {
                    await agent.ProcessTextAsync(e.Text, client.ClientId, CancellationToken.None);
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
                    await agent.ProcessTextAsync(e.Text, client.ClientId, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending text to agent {AgentId}", agent.AgentId);
                }
            }

        }

        private async void OnClientDTMFReceived(object? sender, ConversationDTMFReceivedEventArgs e)
        {
            if (sender is string)
            {
                sender = _clients.Find(c => c.ClientId == (string)sender);
            }

            if (sender is not IConversationClient client)
                return;

            _logger.LogInformation("Received digit from client {ClientId}: {Text}", client.ClientId, e.Digit);

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

            _logger.LogInformation("Client {ClientId} disconnected: {Reason}", client.ClientId, e.Reason);

            // Remove the client
            await RemoveClientAsync(client.ClientId, e.Reason);
        }

        private async void OnAgentAudioGenerated(object? sender, ConversationAudioGeneratedEventArgs e)
        {
            if (sender is string)
            {
                sender = _agents.Find(a => a.AgentId == (string)sender);
            }

            if (sender is not IConversationAgent agent)
                return;

            // Store audio if recording is enabled
            if (_sessionContextData.Timeout.RecordCallAudio)
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
        private async void OnAgentTextResponse(object? sender, ConversationTextGeneratedEventArgs e)
        {
            if (sender is string)
            {
                sender = _agents.Find(a => a.AgentId == (string)sender);
            }

            if (sender is not IConversationAgent agent)
                return;

            _logger.LogInformation("Agent {AgentId} generated text: {Text}", agent.AgentId, e.Text);

            // Create a message
            var message = new ConversationMessage(agent.AgentId, ConversationSenderRole.Agent, e.Text);

            // Add to history
            lock (_messagesLock)
            {
                _messages.Add(message);
            }

            // Create message data for storage
            var messageData = new ConversationMessageData
            {
                SenderId = agent.AgentId,
                Role = ConversationSenderRole.Agent,
                Content = e.Text,
                Timestamp = DateTime.UtcNow
            };

            // Save to repository
            await _conversationStateRepository.AddMessageAsync(_sessionId, messageData);

            // Notify event subscribers
            MessageAdded?.Invoke(this, new ConversationMessageAddedEventArgs(message));

            if (e.OnlySave) return;

            // If target client is specified, send only to that client
            if (!string.IsNullOrEmpty(e.TargetClientId))
            {
                var targetClient = GetClients().FirstOrDefault(c => c.ClientId == e.TargetClientId);
                if (targetClient != null)
                {
                    await targetClient.SendTextAsync(e.Text, CancellationToken.None);
                }
                else
                {
                    _logger.LogWarning("Target client {ClientId} not found for text from agent {AgentId}", e.TargetClientId, agent.AgentId);
                }
                return;
            }

            // Otherwise, broadcast to all clients
            foreach (var client in GetClients())
            {
                try
                {
                    await client.SendTextAsync(e.Text, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending text to client {ClientId}", client.ClientId);
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

            // Log the thought process
            AddLogEntry(ConversationLogLevel.Debug, $"Agent {agent.AgentId} thinking: {e.ThoughtProcess}");
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

            // Log the error
            AddLogEntry(
                e.Severity == ConversationErrorSeverity.Critical
                    ? ConversationLogLevel.Critical
                    : ConversationLogLevel.Error,
                $"Agent {agent.AgentId} error: {e.ErrorMessage}",
                new { Exception = e.Exception?.ToString() }
            );

            // End the session if it's a critical error
            if (e.Severity == ConversationErrorSeverity.Critical)
            {
                EndAsync($"Critical agent error: {e.ErrorMessage}").ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        _logger.LogError(t.Exception, "Error ending session due to critical agent error");
                    }
                });
            }
        }

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