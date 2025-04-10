using IqraCore.Entities.Business;
using IqraCore.Entities.Conversation;
using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Interfaces.Conversation;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Conversation.Agent.AI;
using IqraInfrastructure.Managers.Conversation.Client;
using IqraInfrastructure.Repositories.Conversation;
using IqraInfrastructure.Services;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Conversation
{
    public class ConversationSessionManager
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ConversationSessionManager> _logger;
        private readonly BusinessManager _businessManager;
        private readonly ConversationStateRepository _conversationStateRepository;
        private readonly ConversationAudioRepository _audioStorageManager;

        private readonly string _sessionId;

        private readonly string _callOrWebInitiated;

        private readonly List<IConversationClient> _clients = new();
        private IConversationClient? _primaryClient = null;

        private readonly List<IConversationAgent> _agents = new();
        private IConversationAgent? _primaryAgent = null;

        private readonly List<ConversationMessage> _messages = new();

        private readonly ConversationSessionConfiguration _configuration;

        private BusinessApp _sessionBusinessAppData;
        private BusinessAppRoute _sessionBusinessRouteData;

        private readonly object _clientsLock = new();
        private readonly object _agentsLock = new();
        private readonly object _messagesLock = new();

        private ConversationSessionState _state = ConversationSessionState.Created;
        private DateTime _lastUserActivityTime = DateTime.UtcNow;
        private Timer? _silenceTimer;
        private Timer? _sessionDurationTimer;
        private CancellationTokenSource? _sessionCts;

        public event EventHandler<ConversationSessionStateChangedEventArgs>? StateChanged;
        public event EventHandler<ConversationMessageAddedEventArgs>? MessageAdded;
        public event EventHandler<ConversationDTMFReceivedEventArgs>? DTMFRecieved;
        public event EventHandler<ConversationClientAddedEventArgs>? ClientAdded;
        public event EventHandler<ConversationClientRemovedEventArgs>? ClientRemoved;
        public event EventHandler<ConversationAgentAddedEventArgs>? AgentAdded;
        public event EventHandler<ConversationAgentRemovedEventArgs>? AgentRemoved;

        public string SessionId => _sessionId;
        public ConversationSessionConfiguration Configuration => _configuration;
        public bool IsCallInitiated => _callOrWebInitiated == "call";
        public bool IsWebInitiated => _callOrWebInitiated == "web";
        public string PrimaryClientIdentifier()
        {
            if (_primaryClient != null && _primaryClient.ClientType == ConversationClientType.Telephony) return ((BaseTelephonyConversationClient)_primaryClient).ClientPhoneNumber;
            return "UNKNOWN";
        }

        public ConversationSessionManager(
            string sessionId,
            BusinessManager businessManager,
            ConversationSessionConfiguration configuration,
            ConversationStateRepository conversationStateRepository,
            ConversationAudioRepository audioStorageManager,
            ILoggerFactory loggerFactory,
            string callOrWebInitiated
            )
        {
            _sessionId = sessionId;
            _callOrWebInitiated = callOrWebInitiated;
            _businessManager = businessManager;
            _configuration = configuration;
            _conversationStateRepository = conversationStateRepository;
            _audioStorageManager = audioStorageManager;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<ConversationSessionManager>();

            // Create initial conversation state in the repository
            InitalizeConversationConfigurationAsync().Wait();
            InitializeConversationStateAsync().Wait();
        }

        private async Task InitalizeConversationConfigurationAsync()
        {
            var businessAppData = await _businessManager.GetUserBusinessAppById(_configuration.BusinessId, "InitalizeConversationConfigurationAsync");
            if (!businessAppData.Success)
            {
                _logger.LogError("Business app data not found for business ID {BusinessId}", _configuration.BusinessId);
                throw new InvalidOperationException($"Business app data not found for business ID {_configuration.BusinessId}");
            }
            _sessionBusinessAppData = businessAppData.Data;

            var businessRouteData = businessAppData.Data.Routings.Find(r => r.Id == _configuration.RouteId);
            if (businessRouteData == null)
            {
                _logger.LogError("Business route data not found for business ID {BusinessId} and route ID {RouteId}", _configuration.BusinessId, _configuration.RouteId);
                throw new InvalidOperationException($"Business route data not found for business ID {_configuration.BusinessId} and route ID {_configuration.RouteId}");
            }
            _sessionBusinessRouteData = businessRouteData;
        }

        private async Task InitializeConversationStateAsync()
        {
            var conversationState = new ConversationState
            {
                Id = _sessionId,
                BusinessId = _configuration.BusinessId,
                RouteId = _configuration.RouteId,
                QueueId = _configuration.QueueId,
                Status = ConversationSessionState.Created,
                StartTime = DateTime.UtcNow,
                LastActivityTime = DateTime.UtcNow
            };

            await _conversationStateRepository.CreateAsync(conversationState);
            _logger.LogInformation("Initialized conversation state with ID {SessionId}", _sessionId);
        }

        public async Task<bool> AddClientAsync(IConversationClient client, int SampleRate, int Channels, int bitsPerSample)
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
                    SampleRate = SampleRate,
                    Channels = Channels,
                    BitsPerSample = 16,
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

            _logger.LogInformation("Removed client {ClientId} from session {SessionId}: {Reason}", clientId, _sessionId, reason);

            // If there are no more clients, end the session
            if (_clients.Count == 0 && _state == ConversationSessionState.Active)
            {
                await EndAsync("All clients disconnected");
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

        public bool SetPrimaryClient(string clientId)
        {
            lock (_clientsLock)
            {
                _primaryClient = _clients.FirstOrDefault(c => c.ClientId == clientId);
                return _primaryClient != null;
            }
        }

        public async Task<bool> AddAgentAsync(IConversationAgent agent, ConversationAgentConfiguration configuration)
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

                agent.Thinking += OnAgentThinking;
                agent.ErrorOccurred += OnAgentErrorOccurred;

                _agents.Add(agent);
            }

            // Initialize the agent with the provided configuration
            await agent.InitializeAsync(configuration, _sessionBusinessAppData, _sessionBusinessRouteData, CancellationToken.None);

            // Create agent info in the database
            var agentInfo = new ConversationAgentInfo
            {
                AgentId = agent.AgentId,
                AgentType = agent.AgentType,
                JoinedAt = DateTime.UtcNow,
                AudioInfo = new ConversationMemberAudioInfo()
                {
                    SampleRate = configuration.SampleRate,
                    Channels = configuration.Channels,
                    BitsPerSample = configuration.BitsPerSample,
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

            _logger.LogInformation("Removed agent {AgentId} from session {SessionId}: {Reason}", agentId, _sessionId, reason);
            return true;
        }

        public bool SetPrimaryAgent(string agentId)
        {
            lock (_agentsLock)
            {
                _primaryAgent = _agents.FirstOrDefault(a => a.AgentId == agentId);
                return _primaryAgent != null;
            }
        }

        public IReadOnlyList<IConversationAgent> GetAgents()
        {
            lock (_agentsLock)
            {
                return _agents.ToList().AsReadOnly();
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_state != ConversationSessionState.Created)
            {
                _logger.LogWarning("Cannot start session {SessionId} because it is in state {State}", _sessionId, _state);
                return;
            }

            // Create a cancellation token for the session
            _sessionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Update state
            await UpdateStateAsync(ConversationSessionState.Starting, "Session starting");

            // Initialize clients
            var tasks = new List<Task>();
            foreach (var client in _clients)
            {
                tasks.Add(
                    Task.Run(
                        async () =>
                        {
                            await Task.Delay(_sessionBusinessRouteData.Configuration.PickUpDelayMS, _sessionCts.Token);
                            await client.ConnectAsync(_sessionCts.Token);
                        }
                    )
                );
            }
            await Task.WhenAll(tasks);

            // Start silence and max duration detection timer
            StartTimers();

            // Update state
            await UpdateStateAsync(ConversationSessionState.Active, "Session active");

            var agentsNotify = _agents.Select(async (agent) =>
                {
                    try
                    {
                        return agent.NotifyConversationStarted().WaitAsync(_sessionCts.Token);
                    }
                    catch (Exception ex)
                    {
                        return Task.CompletedTask;
                    }
                }
            );
            await Task.WhenAll(agentsNotify);

            _logger.LogInformation("Session {SessionId} started", _sessionId);
        }

        private void StartTimers()
        {
            // Start silence detection timer
            _silenceTimer = new Timer(CheckSilence, null, _sessionBusinessRouteData.Configuration.NotifyOnSilenceMS, _sessionBusinessRouteData.Configuration.NotifyOnSilenceMS);

            // Start session duration timer
            var maxDurationMs = _sessionBusinessRouteData.Configuration.MaxCallTimeS * 1000;
            _sessionDurationTimer = new Timer(async (state) => { await EndSessionOnMaxDuration(state); }, null, maxDurationMs, Timeout.Infinite);
        }

        private void CheckSilence(object? state)
        {
            var silenceDuration = DateTime.UtcNow - _lastUserActivityTime;

            if (silenceDuration.TotalMilliseconds > _sessionBusinessRouteData.Configuration.EndCallOnSilenceMS)
            {
                _logger.LogInformation("Ending session {SessionId} due to silence timeout", _sessionId);
                EndAsync("Silence timeout reached").ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        _logger.LogError(t.Exception, "Error ending session due to silence");
                    }
                });
            }
            else if (silenceDuration.TotalMilliseconds > _sessionBusinessRouteData.Configuration.NotifyOnSilenceMS)
            {
                _logger.LogDebug("Silence detected in session {SessionId}", _sessionId);

                // Notify agents about silence
                lock (_agentsLock)
                {
                    foreach (var agent in _agents)
                    {
                        // TODO disabled for now
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

        public async Task EndAsync(string reason)
        {
            if (_state == ConversationSessionState.Ended)
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
            await UpdateStateAsync(ConversationSessionState.Ended, reason);

            // Calculate final metrics
            await UpdateFinalMetricsAsync();

            // Run Audio Compilation in the background
            RunAudioCompilationAsync();

            _logger.LogInformation("Session {SessionId} ended: {Reason}", _sessionId, reason);
        }

        private async Task UpdateFinalMetricsAsync()
        {
            try
            {
                var state = await _conversationStateRepository.GetByIdAsync(_sessionId);
                if (state == null) return;

                var metrics = state.Metrics;

                // Calculate duration
                if (state.EndTime.HasValue && state.StartTime != default)
                {
                    metrics.DurationSeconds = (state.EndTime.Value - state.StartTime).TotalSeconds;
                }

                // Update metrics in the repository
                await _conversationStateRepository.UpdateMetricsAsync(_sessionId, metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating final metrics for session {SessionId}", _sessionId);
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

        private async Task UpdateStateAsync(ConversationSessionState newState, string reason)
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

        private void OnClientAudioReceived(object? sender, ConversationAudioReceivedEventArgs e)
        {
            if (sender is string)
            {
                sender = _clients.Find(c => c.ClientId == (string)sender);
            }

            if (sender is not IConversationClient client)
                return;

            _logger.LogDebug("Received audio from client {ClientId}", client.ClientId);

            // Update last activity time for silence detection
            _lastUserActivityTime = DateTime.UtcNow;

            // Store audio if recording is enabled
            if (_sessionBusinessRouteData.Configuration.RecordCallAudio)
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

            _logger.LogDebug("Agent {AgentId} generated audio", agent.AgentId);

            // Store audio if recording is enabled
            if (_sessionBusinessRouteData.Configuration.RecordCallAudio)
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
                    await targetClient.SendAudioAsync(e.AudioData, CancellationToken.None);
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
                    await client.SendAudioAsync(e.AudioData, CancellationToken.None);
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

            _logger.LogDebug("Agent {AgentId} thinking: {ThoughtProcess}", agent.AgentId, e.ThoughtProcess);

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
    }
}