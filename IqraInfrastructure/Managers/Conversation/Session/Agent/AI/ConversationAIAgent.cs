using Humanizer;
using IqraCore.Entities.Business;
using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Conversation.Context;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Conversation.Turn;
using IqraCore.Interfaces.Conversation;
using IqraCore.Interfaces.VAD;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Conversation.Session.Agent.AI.Helpers;
using IqraInfrastructure.Managers.Conversation.Session.Client.Telephony;
using IqraInfrastructure.Managers.Embedding;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.KnowledgeBase;
using IqraInfrastructure.Managers.Languages;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.Rerank;
using IqraInfrastructure.Managers.STT;
using IqraInfrastructure.Managers.Telephony;
using IqraInfrastructure.Managers.TTS;
using IqraInfrastructure.Managers.VAD.Silero;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.KnowledgeBase.Vector;
using IqraInfrastructure.Repositories.RAG;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Conversation.Session.Agent.AI
{
    public class ConversationAIAgent : IConversationAgent
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ConversationAIAgent> _logger;

        private readonly ConversationAgentConfiguration _agentConfiguration;

        // Dependencies
        private readonly ConversationSessionOrchestrator _conversationSessionManager;
        private readonly BusinessManager _businessManager;
        private readonly SystemPromptGenerator _systemPromptGenerator;
        private readonly LanguagesManager _langaugesManager;
        private readonly STTProviderManager _sttProviderManager;
        private readonly TTSProviderManager _ttsProviderManager;
        private readonly LLMProviderManager _llmProviderManager;
        private readonly ScriptExecutionManager _scriptAccessor;
        private readonly BusinessAgentAudioRepository _audioRepository;

        // Agent State & Modules
        private readonly ConversationAIAgentState _agentState;
        private readonly ConversationAIAgentAudioInput _audioInputHandler;
        private readonly ConversationAIAgentSTTHandler _sttHandler;
        private readonly ConversationAIAgentLLMHandler _llmHandler;
        private readonly ConversationAIAgentToolExecutor _toolExecutor;
        private readonly ConversationAIAgentAudioOutput _audioOutputHandler;
        private readonly ConversationAIAgentTurnAndInterruptionManager _turnManager;
        private readonly ConversationAIAgentDTMFSessionManager _dtmfSessionManager;
        private readonly CustomToolExecutionHelper _customToolHelper;
        private readonly SendSMSToolExecutionHelper _sendSMSToolExecutionHelper;
        private readonly ConversationAIAgentRAGManager _ragManager;
        private readonly ConversationAIAgentVoicemailDetector _voicemailDetector;

        // Master Cancellation Token
        private CancellationTokenSource _conversationCTS = new();

        // Multi Language
        private string cachedMultiLanguagePlayMessage = string.Empty;
        private bool _isConversationStarted = false;

        // Public Interface
        public string AgentId => _agentState.AgentId;
        public ConversationAgentType AgentType => ConversationAgentType.AI;
        public ConversationAgentConfiguration AgentConfiguration => _agentConfiguration;
        public ConversationAIAgentState AgentState => _agentState;

        // Events
        public event EventHandler<ConversationAudioGeneratedEventArgs>? AudioGenerated;
        public event EventHandler<object?> ClearBufferedAudio;
        public event EventHandler<ConversationAgentThinkingEventArgs>? Thinking; // TODO: Wire this up if needed
        public event EventHandler<ConversationAgentErrorEventArgs>? ErrorOccurred;

        public ConversationAIAgent(
            ILoggerFactory loggerFactory,
            ConversationSessionOrchestrator sessionManager,
            string agentId, // Agent ID passed in
            ConversationAgentConfiguration agentConfiguration,

            BusinessManager businessManager,
            SystemPromptGenerator systemPromptGenerator,
            STTProviderManager sttProviderManager,
            TTSProviderManager ttsProviderManager,
            LLMProviderManager llmProviderManager,
            LanguagesManager languagesManager,
            BusinessAgentAudioRepository audioRepository,
            IntegrationsManager integrationManager,
            ModemTelManager modemTelManager,
            TwilioManager twilioManager,
            TTSAudioCacheManager ttsAudioCacheManager,
            IServiceProvider serviceProvider
        )
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<ConversationAIAgent>();

            _agentConfiguration = agentConfiguration;

            // Store dependencies
            _conversationSessionManager = sessionManager;
            _businessManager = businessManager;
            _systemPromptGenerator = systemPromptGenerator;
            _sttProviderManager = sttProviderManager;
            _ttsProviderManager = ttsProviderManager;
            _llmProviderManager = llmProviderManager;
            _langaugesManager = languagesManager;
            _audioRepository = audioRepository; 

            // Create shared state
            _agentState = new ConversationAIAgentState(agentId, _conversationCTS.Token);
            _agentState.AgentConfiguration = _agentConfiguration;

            // Instantiate Helper Modules
            _scriptAccessor = new ScriptExecutionManager(loggerFactory.CreateLogger<ScriptExecutionManager>()); // Now primarily data access
            _customToolHelper = new CustomToolExecutionHelper(loggerFactory); // New helper for tool execution
            _sendSMSToolExecutionHelper = new SendSMSToolExecutionHelper(loggerFactory, integrationManager, modemTelManager, twilioManager);

            // Instantiate Core Modules
            _ragManager = new ConversationAIAgentRAGManager(
                _loggerFactory,
                _agentState,
                _businessManager,
                serviceProvider.GetRequiredService<KnowledgeBaseVectorRepository>(),
                serviceProvider.GetRequiredService<RAGKeywordStore>(),
                serviceProvider.GetRequiredService<EmbeddingProviderManager>(),
                serviceProvider.GetRequiredService<BusinessKnowledgeBaseDocumentRepository>(),
                serviceProvider.GetRequiredService<KnowledgeBaseCollectionsLoadManager>(),
                serviceProvider.GetRequiredService<EmbeddingCacheManager>(),
                serviceProvider.GetRequiredService<RerankProviderManager>(),
                serviceProvider.GetRequiredService<LLMProviderManager>(),
                _conversationSessionManager.SessionId
            );
            _dtmfSessionManager = new ConversationAIAgentDTMFSessionManager(_loggerFactory, _agentState);
            _audioOutputHandler = new ConversationAIAgentAudioOutput(_loggerFactory, _agentState, _ttsProviderManager, _audioRepository, _businessManager, ttsAudioCacheManager, _conversationSessionManager);
            _llmHandler = new ConversationAIAgentLLMHandler(_loggerFactory, _agentState, _llmProviderManager, _businessManager, _systemPromptGenerator, _conversationSessionManager, _ragManager);
            _toolExecutor = new ConversationAIAgentToolExecutor(_loggerFactory, _conversationSessionManager, _agentState, _scriptAccessor, _customToolHelper, _dtmfSessionManager, _sendSMSToolExecutionHelper, _ragManager);
            _turnManager = new ConversationAIAgentTurnAndInterruptionManager(_loggerFactory, _llmHandler, _audioOutputHandler, _agentState, _llmProviderManager, _businessManager);
            _audioInputHandler = new ConversationAIAgentAudioInput(_loggerFactory, _agentState);
            _sttHandler = new ConversationAIAgentSTTHandler(_loggerFactory, _agentState, _sttProviderManager, _businessManager);
            if (_conversationSessionManager.IsOutboundCall)
            {
                _voicemailDetector = new ConversationAIAgentVoicemailDetector(_loggerFactory, _agentState, _sttProviderManager, _llmProviderManager);
            }

            // Wire up Events between Modules and Orchestrator
            WireUpEvents();
        }

        // Initalize
        private void WireUpEvents()
        {
            // Audio Output
            _audioOutputHandler.AudioChunkGenerated += (sender, args) => AudioGenerated?.Invoke(this, args);
            _audioOutputHandler.TurnUpdate += OnTurnUpdated;
            _audioOutputHandler.AgentResponsePlaybackComplete += OnAgentResponsePlaybackComplete;
            _audioOutputHandler.OnAudioBufferCleared += (sender, args) => ClearBufferedAudio?.Invoke(this, args);

            // STT Handler 
            _sttHandler.TranscriptionReceived += (text, isFinal) =>
            {
                _turnManager.ProcessTranscriptionForTurnAnalysis(text, isFinal);
            };

            // Turn End/Interruption
            _turnManager.NewTurnCreated += OnNewTurnCreated;
            _turnManager.UserTurnFinalized += OnUserTurnFinalizedAsync;
            _turnManager.VerifiedInterruptionOccurred += OnVerifiedInterruptionOccurredAsync;
            _turnManager.AgentShouldPause += OnAgentShouldPauseAsync;
            _turnManager.AgentShouldResume += OnAgentShouldResumeAsync;

            // LLM Handler
            _llmHandler.SynthesizeTextSegmentRequested += OnSynthesizeTextSegmentRequested;
            _llmHandler.SystemToolExecutionRequested += OnSystemToolExecutionRequested;
            _llmHandler.CustomToolExecutionRequested += OnCustomToolExecutionRequested;

            // Tool Executor
            _toolExecutor.TurnUpdate += OnTurnUpdated;
            _toolExecutor.ToolResultAvailable += OnToolResultAvailable;
            _toolExecutor.PlaySpeechRequested += OnPlaySpeechRequested;
            _toolExecutor.EndConversationRequested += OnEndConversationRequested;
            //_toolExecutor.TransferToAIAgentRequested += OnTransferToAIAgentRequested;
            //_toolExecutor.TransferToHumanAgentRequested += OnTransferToHumanAgentRequested;
            _toolExecutor.SendDTMFRequested += async (digits) =>
            {
                IConversationClient? client = _conversationSessionManager.PrimaryClient;
                if (client != null && client is BaseTelephonyConversationClient telephonyClient)
                {
                    await telephonyClient.SendDTMFAsync(digits, _conversationCTS.Token);
                }
            };

             // DTMF Session Manager
             _dtmfSessionManager.SessionEnded += OnDtmfSessionEnded;

            // Voicemail Detection
            if(_conversationSessionManager.IsOutboundCall)
            {
                _voicemailDetector.OnStopAgentSpeaking += async () =>
                {
                    await PauseAgentOnVoicemailDetectedAsync();
                };

                _voicemailDetector.OnEndCallorLeaveMessageRecieved += async () =>
                {
                    await EndOrLeaveMessageEndOnVoicemailDetectedAsync();
                };
            }
        }
        public async Task InitializeAsync()
        {
            if (_agentState.IsInitialized)
            {
                _logger.LogWarning("AI Agent {AgentId} is already initialized.", AgentId);
                return;
            }

            CancellationToken cancellationToken = _conversationSessionManager.CancellationTokenSource.Token;
            _conversationCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                BusinessApp businessAppData = _conversationSessionManager.BusinessApp!;
                ConversationSessionContext contextData = _conversationSessionManager.Context!;
                

                // Populate Initial State
                _agentState.BusinessApp = businessAppData;
                _agentState.CurrentSessionContext = contextData;
                _agentState.CurrentLanguageCode = contextData.Language.DefaultLanguageCode; // Initial language
                _agentState.BusinessAppAgent = businessAppData.Agents.Find(a => a.Id == contextData.Agent.SelectedAgentId);
                if (_agentState.BusinessAppAgent == null)
                {
                    throw new InvalidOperationException($"Business app agent {contextData.Agent.SelectedAgentId} not found");
                }
                _agentState.BackgroundMusicVolume = (float)((float)(_agentState.BusinessAppAgent.Settings?.BackgroundAudioVolume ?? 30) / 100); // Get from config

                // Silero Vad
                var vadOptions = new VadOptions
                {
                    AudioEncodingType = _agentState.AgentConfiguration.AudioEncodingType,
                    SampleRate = _agentState.AgentConfiguration.SampleRate,
                    BitsPerSample = _agentState.AgentConfiguration.BitsPerSample
                };
                _agentState.SileroVadCore = new SileroVadCore(_loggerFactory.CreateLogger<SileroVadCore>(), vadOptions, _conversationCTS.Token);

                // Initialize Modules
                await _llmHandler.InitializeAsync();
                await _audioOutputHandler.InitializeAsync(_conversationCTS.Token);
                await _sttHandler.InitializeAsync();
                await _scriptAccessor.LoadScriptAsync(
                    _agentState.BusinessApp,
                    _agentState.CurrentSessionContext,
                    _agentState.CurrentLanguageCode
                );
                await _toolExecutor.InitializeAsync();
                await _turnManager.InitializeAsync(_conversationCTS.Token);
                _audioInputHandler.InitializeAsync(_conversationCTS.Token);
                await _ragManager.InitializeAsync(_conversationCTS.Token);

                if (_conversationSessionManager.IsOutboundCall)
                {
                    await _voicemailDetector.InitializeAsync(_conversationSessionManager.CallQueueTelephonyCampaignData!.VoicemailDetection, _conversationCTS.Token);
                }

                _agentState.IsInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing AI Agent {AgentId}", AgentId);
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error initializing agent: " + ex.Message, ex));

                await ShutdownAsync("Initialization failed"); // Attempt cleanup
                throw; // Re-throw to signal failure
            }
        }

        // Agent Management
        public async Task NotifyConversationStarted()
        {
            if (!_agentState.IsInitialized || _conversationCTS.IsCancellationRequested)
            {
                _logger.LogWarning("Agent {AgentId}: Cannot notify start - not initialized or shutting down.", AgentId);
                return;
            }

            // Check if language selection is required
            bool requiresLanguageSelection = _agentState.CurrentSessionContext?.Language.MultiLanguageEnabled == true &&
                                            _agentState.CurrentSessionContext.Language.EnabledMultiLanguages?.Count > 1;

            // Initial Empty Turn
            var newTurn = new ConversationTurn
            {
                Type = ConversationTurnType.System,
                SystemInput = new ConversationTurnSystemInput()
                {
                    Type = "ConversationStarted"
                },
                Response = new ConversationTurnAgentResponse
                {
                    AgentId = _agentState.BusinessAppAgent!.Id,
                    Type = ConversationTurnAgentResponseType.Speech,
                },
                Status = ConversationTurnStatus.AgentProcessing
            };
            await OnNewTurnCreated(newTurn);
            _turnManager.SetUserTurnActive();

            // Start Services
            _sttHandler.StartTranscription();
            _audioOutputHandler.StartProcessingAudioTask();
            _audioInputHandler.StartProcessingAudioTask();
            _agentState.SileroVadCore?.StartAudioProcessingTask();
            if (_conversationSessionManager.IsOutboundCall)
            {
                _ = _voicemailDetector.StartAsync();
            }
            _isConversationStarted = true;

            if (requiresLanguageSelection)
            {
                await SetupLanguageSelectionViaDTMFAsync(newTurn);
            }
            else
            {
                await BeginAgentConversationFlowAsync(newTurn);
            }
        }
        public async Task NotifyMaxDurationReached()
        {
            if (!_agentState.IsInitialized) return;

            _agentState.IsAcceptingSTTAudio = false;

            // TODO end the current turn properly
            await _llmHandler.CancelCurrentLLMTaskAsync();
            await _audioOutputHandler.CancelCurrentSpeechPlaybackAsync();

            var finalTurn = new ConversationTurn
            {
                Sequence = (await _conversationSessionManager.GetTurnsAsync()).Count + 1,
                SystemInput = new ConversationTurnSystemInput()
                {
                    Type = "MaxDurationReached"
                },
                Response = new ConversationTurnAgentResponse()
                {
                    AgentId = _agentState.BusinessAppAgent!.Id,
                    Type = ConversationTurnAgentResponseType.Speech
                },
                Status = ConversationTurnStatus.AgentProcessing
            };
            _turnManager.SetUserTurnActive();

            // TODO play the max duration reached message and end the call
        }
        public async Task ShutdownAsync(string reason)
        {
            if (!_agentState.IsInitialized && _conversationCTS.IsCancellationRequested)
            {
                _logger.LogInformation("AI Agent {AgentId} shutdown already in progress or completed.", AgentId);
                return;
            }
            _logger.LogInformation("AI Agent {AgentId} shutting down. Reason: {Reason}", AgentId, reason);

            // Signal shutdown start
            _agentState.IsInitialized = false;
            _agentState.IsAcceptingSTTAudio = false;
            if (!_conversationCTS.IsCancellationRequested)
            {
                _conversationCTS.Cancel();
            }

            _audioInputHandler?.Dispose();

            _turnManager.Dispose();

            await (_llmHandler?.CancelCurrentLLMTaskAsync() ?? Task.CompletedTask);
            _llmHandler?.Dispose();

            _sttHandler?.StopTranscription();
            _sttHandler?.Dispose();

            _turnManager?.Dispose();

            _agentState.SileroVadCore?.Dispose();

            _dtmfSessionManager?.Dispose();

            await (_audioOutputHandler?.CancelCurrentSpeechPlaybackAsync() ?? Task.CompletedTask);
            _audioOutputHandler?.Dispose();

            if (_conversationSessionManager.IsOutboundCall)
            {
                _voicemailDetector?.Dispose();
            }

            await (_ragManager?.DisposeAsync() ?? ValueTask.CompletedTask);

            // Dispose Tool Executor? (If it holds resources) TODO > might need to cancel current httpclient ongoign request and dispose
            // _toolExecutor?.Dispose(); // If it implements IDisposable

            AudioGenerated = null;
            ClearBufferedAudio = null;
            Thinking = null;
            ErrorOccurred = null;

            _conversationCTS?.Dispose();

            _logger.LogInformation("AI Agent {AgentId} shut down complete.", AgentId);
        }
        public async Task FinalizeCurrentTurn(ConversationTurnStatus finalStatus)
        {
            if (_agentState.CurrentTurn != null)
            {
                var turnToFinalize = _agentState.CurrentTurn;
                turnToFinalize.Status = finalStatus;
                turnToFinalize.CompletedAt = DateTime.UtcNow;

                await _conversationSessionManager.NotifyTurnUpdated(turnToFinalize);
            }

            _turnManager.ResetForNewTurn();
        }

        // Agent Input Management
        public async Task ProcessAudioAsync(byte[] audioData, string clientId, CancellationToken cancellationToken)
        {
            if (!_agentState.IsInitialized || _conversationCTS.IsCancellationRequested || !_isConversationStarted) return;

            // Set client ID (important for multi-participant, maybe less so for 1-1)
            // todo dont know what this is used for, might be problematic if turn by turn is not enabled?
            // insetad of setting current client id, we should have a proper structure of client id + data being sent
            _agentState.CurrentClientId = clientId;

            _agentState.SileroVadCore?.ProcessAudio(audioData);
            _turnManager.BufferAudioForMlAnalysis(audioData);
            _audioInputHandler.QueueAudioChunk(audioData, cancellationToken);
            if (_conversationSessionManager.IsOutboundCall && !_voicemailDetector.HasServiceEnded)
            {
                _voicemailDetector.BufferAudio(audioData);
            }
        }
        public async Task ProcessTextAsync(string text, string? clientId, CancellationToken cancellationToken)
        {
            if (clientId != null)
            {
                _agentState.CurrentClientId = clientId;
            }

            await _turnManager.ProcessDirectTextInputAsync(text, cancellationToken);
        }
        public async Task ProcessDTMFAsync(string digit, string? clientId, CancellationToken cancellationToken)
        {
            if (!_agentState.IsInitialized || _conversationCTS.IsCancellationRequested) return;

            if (clientId != null) _agentState.CurrentClientId = clientId;

            // Pass digit to the session manager
            _dtmfSessionManager.ProcessDigit(digit);

            // No need for language selection logic here anymore, it's handled by OnDtmfSessionEnded
        }
        
        // Multi Language
        private async Task SetupLanguageSelectionViaDTMFAsync(ConversationTurn turn)
        {
            try
            {
                await HandleLanguageSelectionMessagePlaying(turn);

                var dtmfConfig = new DTMFSessionConfig
                {
                    AssociatedNodeId = "internal::language_selection",
                    MaxLength = 1,
                    TerminatorChar = null,
                    InterDigitTimeoutSeconds = 5,
                    MaxSessionDurationSeconds = 30
                };

                bool started = _dtmfSessionManager.StartSession(dtmfConfig, turn);
                if (started)
                {
                    _agentState.IsAwaitingLanguageSelectionInput = true;
                }
                else
                {
                    _logger.LogError("Agent {AgentId}: Failed to start DTMF session for language selection (already active?).", _agentState.AgentId);
                    // Fallback? Maybe proceed with default language?
                    await BeginAgentConversationFlowAsync(turn);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {AgentId}: Error during language selection setup. Proceeding with default.", _agentState.AgentId);
                await BeginAgentConversationFlowAsync(turn);
            }
        } 
        private async Task HandleLanguageSelectionMessagePlaying(ConversationTurn turn)
        {
            if (string.IsNullOrWhiteSpace(cachedMultiLanguagePlayMessage))
            {
                var availableLangs = _agentState.CurrentSessionContext!.Language.EnabledMultiLanguages!;
                int langCount = availableLangs.Count;

                for (int i = 0; i < langCount; i++)
                {
                    var languageInfo = availableLangs[i];
                    var languageDataResult = await _langaugesManager.GetLanguageByCode(languageInfo.LanguageCode);
                    var languageLocaleName = languageDataResult.Success ? languageDataResult.Data!.Name : languageInfo.LanguageCode;

                    // Consider culture for number words if needed: .ToWords(new System.Globalization.CultureInfo("en-US"))
                    string numberWord = (i + 1).ToWords();

                    string builtMessage = languageInfo.MessageToPlay
                       .Replace("{number}", numberWord, StringComparison.OrdinalIgnoreCase)
                       .Replace("{name}", languageLocaleName, StringComparison.OrdinalIgnoreCase);

                    cachedMultiLanguagePlayMessage += $"\n{builtMessage}";
                }

                cachedMultiLanguagePlayMessage = cachedMultiLanguagePlayMessage.Trim();
            }

            await _audioOutputHandler.SynthesizeAndPlayBlockingAsync(turn, cachedMultiLanguagePlayMessage, _conversationCTS.Token);
        }
        private async Task HandleLanguageChangeRequestAsync(string newLanguageCode)
        {
            if (!_agentState.IsInitialized) return; // Should not happen

            // Update State
            _agentState.CurrentLanguageCode = newLanguageCode;
            _agentState.IsAcceptingSTTAudio = false;

            // Cancel ongoing operations? Optional, but safer.
            _dtmfSessionManager.CancelSession("Language Change");
            await _llmHandler.CancelCurrentLLMTaskAsync();
            await _audioOutputHandler.CancelCurrentSpeechPlaybackAsync();

            // Re-initialize language-dependent modules
            try
            {
                await _llmHandler.ReInitializeForLanguageAsync();
                await _sttHandler.ReInitializeForLanguageAsync();
                await _audioOutputHandler.ReInitializeForLanguageAsync();
                await _toolExecutor.ReInitializeForLanguageAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {AgentId}: Failed to re-initialize modules for language {LanguageCode}", AgentId, newLanguageCode);
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs($"Failed to switch language: {ex.Message}", ex));
                // What state are we in now? Might need to shut down. TODO, this should not happen tho
                await ShutdownAsync("Language change failed");
            }
        }

        // Begin Conversation
        private async Task BeginAgentConversationFlowAsync(ConversationTurn turn)
        {
            if (_agentState.IsBackgroundMusicLoaded)
            {
                _agentState.IsBackgroundMusicEnabled = true; // Logic to start/mix music is in AudioOutputHandler
            }

            // Greeting / Opening depends on configuration
            if (_agentState.BusinessAppAgent?.Utterances.OpeningType == BusinessAppAgentOpeningType.AgentFirst)
            {
                string? openingMessage = null;
                if (_agentState.BusinessAppAgent.Utterances.GreetingMessage?.TryGetValue(_agentState.CurrentLanguageCode, out openingMessage) == true && !string.IsNullOrEmpty(openingMessage))
                {
                    foreach (var dynamicVariable in _agentState.CurrentSessionContext.DynamicVariables ?? new Dictionary<string, string>())
                    {
                        openingMessage = openingMessage.Replace(("{{" + dynamicVariable.Key + "}}"), dynamicVariable.Value);
                    }

                    // Format for LLM history, synthesize raw text
                    string llmHistoryMessage = "response_to_customer: " + openingMessage;
                    _agentState.LLMService?.AddAssistantMessage(llmHistoryMessage); // Add to history
                    await _audioOutputHandler.SynthesizeAndPlayBlockingAsync(turn, openingMessage, _conversationCTS.Token);
                }
                else
                {
                    _logger.LogWarning("Agent {AgentId}: AgentFirst opening configured but no greeting message found for language {LangCode}.", AgentId, _agentState.CurrentLanguageCode);
                }
            }
            else if (_agentState.BusinessAppAgent?.Utterances.OpeningType == BusinessAppAgentOpeningType.UserFirst)
            {
                _logger.LogDebug("Agent {AgentId}: Waiting for user to speak first.", AgentId);
                // Optional: Add a silent initial prompt to LLM history?
                // string openingMessage = "User starts the conversation."; // Example internal note
                // _agentState.LLMService?.AddAssistantMessage(openingMessage); // Or maybe not needed
            }

            // Enable listening after potential greeting
            await FinalizeCurrentTurn(ConversationTurnStatus.Completed);
            _agentState.IsAcceptingSTTAudio = true;
            // TODO: Enable VAD if needed (_vadService.Start() ?)
        }

        // Turn Event Handlers
        private async Task OnNewTurnCreated(ConversationTurn newTurn)
        {
            // The agent is now the authority for the sequence number.
            var turnsInDb = await _conversationSessionManager.GetTurnsAsync();
            newTurn.Sequence = turnsInDb.Count + 1;

            _agentState.PreviousTurn = _agentState.CurrentTurn;
            _agentState.CurrentTurn = newTurn;

            await _conversationSessionManager.NotifyTurnStarted(_agentState.CurrentTurn);
        }
        private async void OnTurnUpdated(object? sender, ConversationTurn turn)
        {
            await _conversationSessionManager.NotifyTurnUpdated(turn);
        }
        private async Task OnUserTurnFinalizedAsync(ConversationTurn turn)
        {
            if (!_agentState.IsInitialized || _conversationCTS.IsCancellationRequested) return;

            await _conversationSessionManager.NotifyTurnUpdated(turn);

            try
            {
                _agentState.CurrentTurn!.Status = ConversationTurnStatus.AgentProcessing;
                await _conversationSessionManager.NotifyTurnUpdated(_agentState.CurrentTurn);

                await _llmHandler.CancelCurrentLLMTaskAsync();
                await _audioOutputHandler.CancelCurrentSpeechPlaybackAsync();

                await _llmHandler.ProcessUserTurnAsync(turn, _conversationCTS.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing finalized user turn {TurnId}", turn.Id);
                await FinalizeCurrentTurn(ConversationTurnStatus.Error);
            }
        }

        // LLM Handler
        private async Task OnSynthesizeTextSegmentRequested(ConversationTurn turn, string textSegment, bool markTurnAsCompleteAfterThis)
        {
            await _audioOutputHandler.SynthesizeAndQueueSpeechAsync(turn, textSegment, markTurnAsCompleteAfterThis, _conversationCTS.Token);
        }
        private async Task OnSystemToolExecutionRequested(ConversationTurn turn)
        {
            await _toolExecutor.HandleSystemToolAsync(turn, _conversationCTS.Token);
        }
        private async Task OnCustomToolExecutionRequested(ConversationTurn turn)
        {
            await _toolExecutor.HandleCustomToolAsync(turn, _conversationCTS.Token);
        }

        // Tool Executor Handler
        private async Task OnToolResultAvailable(ConversationTurn turnWithResult)
        {
            if (turnWithResult.Response.Type != ConversationTurnAgentResponseType.SystemTool &&
                turnWithResult.Response.Type != ConversationTurnAgentResponseType.CustomTool
            ) {
                _logger.LogError("Agent {AgentId}: Invalid turn response type for tool result {TurnId} recieved for processing on tool result", _agentState.AgentId, turnWithResult.Id);
                return;
            }

            if (turnWithResult.Type == ConversationTurnType.ToolResult)
            {
                var resultOfTurn = await _conversationSessionManager.GetTurnAsync(turnWithResult.ToolResultInput!.ResultOfTurnId);
                if (resultOfTurn == null)
                {
                    _logger.LogError("Agent {AgentId}: Tool result for turn {TurnId} not found.", _agentState.AgentId, turnWithResult.Id);
                    return;
                }

                if (_agentState.IsVoicemailDetected)
                {
                    if (
                        resultOfTurn.Type != ConversationTurnType.System ||
                        resultOfTurn.SystemInput?.Type != "VoicemailDetected"
                    )
                    {
                        return;
                    }
                }
            }    

            await FinalizeCurrentTurn(ConversationTurnStatus.Completed);
            var newToolResultTurn = new ConversationTurn()
            {
                Type = ConversationTurnType.ToolResult,
                Status = ConversationTurnStatus.AgentProcessing,
                ToolResultInput = new ConversationTurnToolResultInput()
                {
                    ResultOfTurnId = turnWithResult.Id,
                },
                Response = new ConversationTurnAgentResponse()
                {
                    AgentId = _agentState.BusinessAppAgent!.Id
                }
            };
            await OnNewTurnCreated(newToolResultTurn);
            _turnManager.SetUserTurnActive();

            await _llmHandler.CancelCurrentLLMTaskAsync();
            await _audioOutputHandler.CancelCurrentSpeechPlaybackAsync();

            await _llmHandler.ProcessToolResultAsync(newToolResultTurn, turnWithResult, _conversationCTS.Token);
        }
        private async Task OnPlaySpeechRequested(ConversationTurn turn, string message, CancellationToken token)
        {
            await _audioOutputHandler.SynthesizeAndPlayBlockingAsync(turn, message, token);
        }
        private async Task OnEndConversationRequested(ConversationTurn turn)
        {
            string? reason = turn.Response.ToolExecution?.ReasonForExecution ?? "Agent requested end of conversation, no reason provided.";
            await FinalizeCurrentTurn(ConversationTurnStatus.Completed); 

            var resultOfTurn = await _conversationSessionManager.GetTurnAsync(turn.ToolResultInput!.ResultOfTurnId);
            if (resultOfTurn.Type == ConversationTurnType.System || resultOfTurn.SystemInput?.Type == "VoicemailDetected")
            {
                await _conversationSessionManager.EndAsync(reason, ConversationSessionEndType.VoicemailDetected);
            }
            else
            {
                await _conversationSessionManager.EndAsync(reason, ConversationSessionEndType.AgentEndedCall);
            } 
        }

        // Audio Output Handlers
        private async void OnAgentResponsePlaybackComplete(object? sender, ConversationTurn turn)
        {
            var currentTurnText = string.Join(" ", turn.Response.SpokenSegments.Select(x => x.Text).ToArray());
            _agentState.LLMService!.AddAssistantMessage($"response_to_customer: {currentTurnText}");

            await FinalizeCurrentTurn(ConversationTurnStatus.Completed);
        }

        // Event Handlers
        private async void OnDtmfSessionEnded(object? sender, DTMFSessionEventArgs args)
        {
            if (_conversationCTS.IsCancellationRequested) return; // Don't process if shutting down

            // --- Handle Language Selection ---
            if (args.NodeId == "internal::language_selection" && _agentState.IsAwaitingLanguageSelectionInput)
            {
                if (args.Reason== DTMFSessionEndReason.Cancelled)
                {
                    return;
                }

                _dtmfSessionManager.PauseSession();
                if (args.Reason == DTMFSessionEndReason.CompletedMaxLength)
                {
                    if (
                        int.TryParse(args.CollectedDigits, out int languageIndex) &&
                        _agentState.CurrentSessionContext?.Language.EnabledMultiLanguages != null &&
                        languageIndex > 0 && languageIndex <= _agentState.CurrentSessionContext.Language.EnabledMultiLanguages.Count
                    )
                    {
                        string selectedLanguageCode = _agentState.CurrentSessionContext.Language.EnabledMultiLanguages[languageIndex - 1].LanguageCode;
                        if (selectedLanguageCode != _agentState.CurrentLanguageCode)
                        {
                            await HandleLanguageChangeRequestAsync(selectedLanguageCode);
                        }
                    }
                    else
                    {
                        await _audioOutputHandler.SynthesizeAndPlayBlockingAsync(args.Turn, "Invalid Language Selection.", CancellationToken.None);
                        await HandleLanguageSelectionMessagePlaying(args.Turn);
                        _dtmfSessionManager.ResumeSession();
                        return;
                    }
                }
                else if (args.Reason == DTMFSessionEndReason.TimeoutInterDigit)
                {
                    await _audioOutputHandler.SynthesizeAndPlayBlockingAsync(args.Turn, "Please choose a language.", CancellationToken.None);
                    await HandleLanguageSelectionMessagePlaying(args.Turn);
                    _dtmfSessionManager.ResumeSession();
                    return;
                }

                // TODO ask user how much to wait for after language selection
                // or if they wish for something to be played first
                // could move it to BeginAgentConversationFlowAsync as well after background audio?
                await Task.Delay(2000);

                _agentState.IsAwaitingLanguageSelectionInput = false;
                await BeginAgentConversationFlowAsync(args.Turn);
                _dtmfSessionManager.CancelSession("Language Selected");
                return;
            }
            // --- Handle Regular DTMF Tool Results ---
            else
            {
                bool wasSuccessful = false;
                string resultMessage;
                switch (args.Reason)
                {
                    case DTMFSessionEndReason.CompletedTerminator:
                    case DTMFSessionEndReason.CompletedMaxLength:
                        {
                            var result = $"User entered: {args.CollectedDigits}";
                            bool isEncrypted = _dtmfSessionManager.ActiveSessionConfig?.IsEncrypted ?? false;
                            if (isEncrypted)
                            {
                                result = $"User entered value which is saved encrypted in variable name: {_dtmfSessionManager.ActiveSessionConfig?.SaveEncryptedToVariable}";
                                // TODO SAVE IN VARIABLE
                            }

                            resultMessage = $"Tool result for '{args.NodeId}': DTMF input received successfully: {result}";
                            break;
                        }
                    case DTMFSessionEndReason.TimeoutInterDigit:
                        {
                            var result = $"User entered: {args.CollectedDigits}";
                            bool isEncrypted = _dtmfSessionManager.ActiveSessionConfig?.IsEncrypted ?? false;
                            if (isEncrypted)
                            {
                                result = $"User entered value which is saved encrypted in variable name: {_dtmfSessionManager.ActiveSessionConfig?.SaveEncryptedToVariable}";
                                // TODO SAVE IN VARIABLE
                            }

                            resultMessage = $"Tool result for '{args.NodeId}': DTMF input failed. Timed out waiting between digits. {result}";
                            break;
                        }
                    case DTMFSessionEndReason.TimeoutMaxDuration:
                        {
                            var result = $"User entered: {args.CollectedDigits}";
                            bool isEncrypted = _dtmfSessionManager.ActiveSessionConfig?.IsEncrypted ?? false;
                            if (isEncrypted)
                            {
                                result = $"User entered value which is saved encrypted in variable name: {_dtmfSessionManager.ActiveSessionConfig?.SaveEncryptedToVariable}";
                                // TODO SAVE IN VARIABLE
                            }

                            resultMessage = $"Tool result for '{args.NodeId}': DTMF input failed. Maximum session duration reached. {result}";
                            break;
                        }
                    case DTMFSessionEndReason.Cancelled:
                        resultMessage = $"Tool result for '{args.NodeId}': DTMF input session was cancelled.";
                        break;
                    case DTMFSessionEndReason.Error:
                    default:
                        resultMessage = $"Tool result for '{args.NodeId}': DTMF input session ended with an error.";
                        break;
                }

                await _toolExecutor.FinalizeAndReportToolResult(args.Turn, wasSuccessful, resultMessage);
            }
        }
        private async Task OnAgentShouldPauseAsync()
        {
            await _audioOutputHandler.PausePlaybackAsync();
        }
        private async Task OnAgentShouldResumeAsync()
        {
            await _audioOutputHandler.ResumePlaybackAsync();
        }
        private async Task OnVerifiedInterruptionOccurredAsync(ConversationTurn interruptedTurn)
        {
            await _audioOutputHandler.InterruptCurrentTurnSegment();

            var currentTurnText = string.Join(" ", interruptedTurn.Response.SpokenSegments.Select(x => x.Text).ToArray());
            _agentState.LLMService!.AddAssistantMessage($"response_to_customer: {currentTurnText}");
            await FinalizeCurrentTurn(ConversationTurnStatus.Interrupted);
        }

        // Voicemail Detector Handlers
        private async Task PauseAgentOnVoicemailDetectedAsync()
        {
            _agentState.IsAcceptingSTTAudio = false;
            _agentState.IsVoicemailDetected = true;

            if (_agentState.CurrentTurn == null)
            {
                // critical, should not be possible
                _logger.LogError("Agent {AgentId}: Current turn is null when voicemail detected", AgentId);
                return;
            }

            if (
                _agentState.CurrentTurn.Type == ConversationTurnType.System &&
                _agentState.CurrentTurn.SystemInput?.Type == "ConversationStarted"
                )
            {
                if (_agentState.IsAwaitingLanguageSelectionInput)
                {
                    _dtmfSessionManager.CancelSession("Voicemail detected");
                }
            }

            _turnManager.ResetForNewTurn();

            await _llmHandler.CancelCurrentLLMTaskAsync();
            await _audioOutputHandler.CancelCurrentSpeechPlaybackAsync();

            if (
                (
                    _agentState.CurrentTurn.Response.Type == ConversationTurnAgentResponseType.Speech ||
                    _agentState.CurrentTurn.Response.Type == ConversationTurnAgentResponseType.NotSet ||
                    _agentState.CurrentTurn.Response.Type == ConversationTurnAgentResponseType.Error
                )
                &&
                (
                    _agentState.CurrentTurn.Status != ConversationTurnStatus.Completed ||
                    _agentState.CurrentTurn.Status != ConversationTurnStatus.Interrupted ||
                    _agentState.CurrentTurn.Status != ConversationTurnStatus.Error
                )
            )
            {
                await FinalizeCurrentTurn(ConversationTurnStatus.Interrupted);
            }

            if (
                (
                    _agentState.CurrentTurn.Response.Type == ConversationTurnAgentResponseType.SystemTool ||
                    _agentState.CurrentTurn.Response.Type == ConversationTurnAgentResponseType.CustomTool
                )
                &&
                _agentState.CurrentTurn.Status == ConversationTurnStatus.AgentExecutingTool
            )
            {
                await FinalizeCurrentTurn(ConversationTurnStatus.Interrupted);
            }

            if (
                _agentState.CurrentTurn.Type == ConversationTurnType.ToolResult
                &&
                (
                    _agentState.CurrentTurn.Status != ConversationTurnStatus.Completed ||
                    _agentState.CurrentTurn.Status != ConversationTurnStatus.Interrupted ||
                    _agentState.CurrentTurn.Status != ConversationTurnStatus.Error
                )
            )
            {
                await FinalizeCurrentTurn(ConversationTurnStatus.Interrupted);
            }

            var newTurn = new ConversationTurn
            {
                Type = ConversationTurnType.System,
                SystemInput = new ConversationTurnSystemInput()
                {
                    Type = "VoicemailDetected"
                },
                Response = new ConversationTurnAgentResponse
                {
                    AgentId = _agentState.BusinessAppAgent!.Id,
                    Type = ConversationTurnAgentResponseType.SystemTool,
                    ToolExecution = new ConversationTurnToolExecutionData()
                    {
                        ToolType = ConversationTurnAgentToolType.System,
                        ToolName = "VoicemailDetected",
                        RawLLMInput = "execute_system_function: \"voicemail_detected\""
                    }
                },
                Status = ConversationTurnStatus.AgentProcessing
            };
            await OnNewTurnCreated(newTurn);
        }

        private async Task EndOrLeaveMessageEndOnVoicemailDetectedAsync()
        {
            await FinalizeCurrentTurn(ConversationTurnStatus.Completed);

            var newTurn = new ConversationTurn
            {
                Type = ConversationTurnType.ToolResult,
                ToolResultInput = new ConversationTurnToolResultInput()
                {
                    ResultOfTurnId = _agentState.CurrentTurn!.Id,
                },
                Response = new ConversationTurnAgentResponse
                {
                    AgentId = _agentState.BusinessAppAgent!.Id
                },
                Status = ConversationTurnStatus.AgentProcessing
            };
            await OnNewTurnCreated(newTurn);

            await _llmHandler.CancelCurrentLLMTaskAsync();
            await _audioOutputHandler.CancelCurrentSpeechPlaybackAsync();

            if (_conversationSessionManager.CallQueueTelephonyCampaignData!.VoicemailDetection.LeaveMessageOnDetect)
            {
                _agentState.PreviousTurn!.Response.ToolExecution!.Result = $"voicemail detected, execute end call system function while leaving message: `{_conversationSessionManager.CallQueueTelephonyCampaignData!.VoicemailDetection.MessageToLeave![_agentState.CurrentLanguageCode]}`.";
            }
            else if (_conversationSessionManager.CallQueueTelephonyCampaignData!.VoicemailDetection.EndCallOnDetect)
            {
                _agentState.PreviousTurn!.Response.ToolExecution!.Result = $"voicemail detected, execute end call function without leaving message.";
            }

            await _llmHandler.ProcessToolResultAsync(_agentState.CurrentTurn!, _agentState.PreviousTurn!, _conversationCTS.Token);
        }

        // Disposal
        public void Dispose()
        {
            if (_agentState.IsInitialized || !_conversationCTS.IsCancellationRequested)
            {
                ShutdownAsync("Agent Disposed").Wait(TimeSpan.FromSeconds(5)); // Blocking wait on dispose is risky, use timeout todo why?
            }
        }
    }
}