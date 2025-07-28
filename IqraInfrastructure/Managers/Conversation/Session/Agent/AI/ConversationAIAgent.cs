using Humanizer;
using IqraCore.Entities.Business;
using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Conversation.Context;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Interfaces.Conversation;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Conversation.Session;
using IqraInfrastructure.Managers.Conversation.Session.Agent.AI.Helpers;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.Languages;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.STT;
using IqraInfrastructure.Managers.Telephony;
using IqraInfrastructure.Managers.TTS;
using IqraInfrastructure.Repositories.Business;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Conversation.Session.Agent.AI
{
    public class ConversationAIAgent : IConversationAgent
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ConversationAIAgent> _logger;

        private readonly ConversationAgentConfiguration _agentConfiguration;

        // Dependencies
        private readonly ConversationSession _conversationSessionManager;
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
        private readonly ConversationAIAgentInterruptionManager _interruptionManager;
        private readonly ConversationAIAgentDTMFSessionManager _dtmfSessionManager;
        private readonly CustomToolExecutionHelper _customToolHelper;
        private readonly SendSMSToolExecutionHelper _sendSMSToolExecutionHelper;

        // Master Cancellation Token
        private CancellationTokenSource _conversationCTS = new();

        // Public Interface
        public string AgentId => _agentState.AgentId;
        public ConversationAgentType AgentType => ConversationAgentType.AI;

        public ConversationAgentConfiguration AgentConfiguration => _agentConfiguration;

        // Events
        public event EventHandler<ConversationAudioGeneratedEventArgs>? AudioGenerated;

        public event EventHandler<ConversationTextGeneratedEventArgs>? AgentTextResponse;
        public event EventHandler<ConversationTextReceivedEventArgs>? ClientTextQuery;
        public event EventHandler<object?> ClearBufferedAudio;

        public event EventHandler<ConversationAgentThinkingEventArgs>? Thinking; // TODO: Wire this up if needed
        public event EventHandler<ConversationAgentErrorEventArgs>? ErrorOccurred;

        public ConversationAIAgent(
            ILoggerFactory loggerFactory,
            ConversationSession sessionManager,
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
            TTSAudioCacheManager ttsAudioCacheManager
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
            _dtmfSessionManager = new ConversationAIAgentDTMFSessionManager(_loggerFactory, _agentState);
            _audioOutputHandler = new ConversationAIAgentAudioOutput(_loggerFactory, _agentState, _ttsProviderManager, _audioRepository, _businessManager, ttsAudioCacheManager);
            _llmHandler = new ConversationAIAgentLLMHandler(_loggerFactory, _agentState, _llmProviderManager, _businessManager, _systemPromptGenerator);
            _toolExecutor = new ConversationAIAgentToolExecutor(_loggerFactory, _conversationSessionManager, _agentState, _scriptAccessor, _customToolHelper, _dtmfSessionManager, _sendSMSToolExecutionHelper);
            _audioInputHandler = new ConversationAIAgentAudioInput(_loggerFactory, _agentState);
            _sttHandler = new ConversationAIAgentSTTHandler(_loggerFactory, _agentState, _sttProviderManager, _businessManager);
            _interruptionManager = new ConversationAIAgentInterruptionManager(_loggerFactory, _agentState, _llmProviderManager, _businessManager, _audioOutputHandler, _llmHandler);
            

            // Wire up Events between Modules and Orchestrator
            WireUpEvents();
        }

        private void WireUpEvents()
        {
            // Audio Output -> Orchestrator (Public Events)
            _audioOutputHandler.AudioChunkGenerated += (sender, args) => AudioGenerated?.Invoke(this, args);
            _audioOutputHandler.SpeechPlaybackComplete += OnSpeechPlaybackComplete; // Handle completion signal
            _audioOutputHandler.OnAudioBufferCleared += (sender, args) => ClearBufferedAudio?.Invoke(this, args);

            // STT Handler -> Orchestrator (Process Text)
            _sttHandler.TranscriptionReceived += ProcessTranscriptionResultAsync;

            // LLM Handler -> Orchestrator/Other Modules
            _llmHandler.SynthesizeTextRequested += (text) => _audioOutputHandler.SynthesizeAndQueueSpeechAsync(text, CancellationToken.None); // TODO Use appropriate token

            _llmHandler.AIAgentResponseCompleted += (text) => AgentTextResponse?.Invoke(this, new ConversationTextGeneratedEventArgs(text, _agentState.CurrentClientId, true));
            _llmHandler.TextRecievedForLLMToProcess += (text, clientId) => ClientTextQuery?.Invoke(clientId, new ConversationTextReceivedEventArgs(text, _agentState.AgentId, true));

            _llmHandler.ResponseHandlingComplete += OnLLMResponseHandlingComplete; // May not be needed if SpeechPlaybackComplete is used
            _llmHandler.SystemToolExecutionRequested += (content) => _toolExecutor.HandleSystemToolAsync(content, _conversationCTS.Token, _agentState.CurrentClientId); // Use agent token
            _llmHandler.CustomToolExecutionRequested += (content) => _toolExecutor.HandleCustomToolAsync(content, _conversationCTS.Token); // Use agent token

            // Tool Executor -> LLM / Audio / Orchestrator
            _toolExecutor.ToolResultAvailable += (result) => _llmHandler.ProcessSystemMessageAsync(result, _agentState.CurrentClientId ?? "System", _conversationCTS.Token); // Feed result back
            _toolExecutor.PlaySpeechRequested += (text, token) => _audioOutputHandler.SynthesizeAndPlayBlockingAsync(text, token); // Request blocking speech
            _toolExecutor.EndConversationRequested += async (reason) => {
                 _logger.LogInformation("Agent {AgentId} requested conversation end via tool. Reason: {Reason}", AgentId, reason);
                 // Raise event instead of directly calling session manager? Consistent pattern.
                 // Optionally still call shutdown locally? No, let SessionManager handle it.
                 // await ShutdownAsync(reason);
                 await _conversationSessionManager.EndAsync(reason);
            };
             // NEW: Handle transfer requests from Tool Executor
             _toolExecutor.TransferToAIAgentRequested += async (reason, nodeId) => {
                 _logger.LogInformation("Agent {AgentId} requested transfer to AI Agent via tool. Reason: {Reason}, Node: {NodeId}", AgentId, reason, nodeId);
                 // TODO: Need a way to map nodeId/reason to specific target agent/context if needed.
                 // For now, just signal the end with a transfer reason.
             };
             _toolExecutor.TransferToHumanAgentRequested += async (reason, nodeId) => {
                 _logger.LogInformation("Agent {AgentId} requested transfer to Human Agent via tool. Reason: {Reason}, Node: {NodeId}", AgentId, reason, nodeId);
             };


             // NEW: DTMF Session Manager Events -> Orchestrator/LLM
             _dtmfSessionManager.SessionEnded += OnDtmfSessionEnded;
        }

        private void OnSpeechPlaybackComplete()
        {
            // This is signaled by AudioOutput when the speech queue is empty and the last segment finished playing.
            _logger.LogInformation("Agent {AgentId}: Received signal that speech playback is complete.", AgentId);
            // If LLM was waiting for speech, it can now proceed (if necessary)
            // This helps decouple LLM completion from actual audio finishing.

            // Potentially signal LLM handler if it needs to wait for speech before continuing?
            // Or use to re-enable STT input after agent speaks?
            // If (! _agentState.IsExecutingTool && !_dtmfSessionManager.IsSessionActive) {
            //    _agentState.IsAcceptingSTTAudio = true; // Re-enable listening? Context dependent.
            // }
        }

        private void OnLLMResponseHandlingComplete()
        {
            // This is signaled by LLMHandler when it finishes processing a 'response_to_customer' stream.
            _logger.LogInformation("Agent {AgentId}: Received signal that LLM response handling is complete.", AgentId);
            // Usually followed by waiting for OnSpeechPlaybackComplete.
        }

        public async Task InitializeAsync(BusinessApp businessAppData, ConversationSessionContext contextData, CancellationToken cancellationToken)
        {
            if (_agentState.IsInitialized)
            {
                _logger.LogWarning("AI Agent {AgentId} is already initialized.", AgentId);
                return;
            }
            _logger.LogInformation("AI Agent {AgentId} initializing...", AgentId);

            _conversationCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                // Populate Initial State
                _agentState.BusinessApp = businessAppData;
                _agentState.CurrentSessionContext = contextData;
                _agentState.CurrentConversationType = contextData.Agent.Interruption.Type;
                _agentState.CurrentLanguageCode = contextData.Language.DefaultLanguageCode; // Initial language
                _agentState.BusinessAppAgent = businessAppData.Agents.Find(a => a.Id == contextData.Agent.SelectedAgentId);
                if (_agentState.BusinessAppAgent == null)
                {
                    throw new InvalidOperationException($"Business app agent {contextData.Agent.SelectedAgentId} not found");
                }
                _agentState.BackgroundMusicVolume = (float)((float)(_agentState.BusinessAppAgent.Settings?.BackgroundAudioVolume ?? 30)/100); // Get from config

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
                await _interruptionManager.InitializeAsync(_conversationCTS.Token);
                await _audioInputHandler.InitializeAsync(_conversationCTS.Token);

                _agentState.IsInitialized = true;
                _logger.LogInformation("AI Agent {AgentId} initialized successfully. Lang: {Lang}, Type: {Type}",
                    AgentId, _agentState.CurrentLanguageCode, _agentState.CurrentConversationType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing AI Agent {AgentId}", AgentId);
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error initializing agent: " + ex.Message, ex));

                await ShutdownAsync("Initialization failed"); // Attempt cleanup
                throw; // Re-throw to signal failure
            }
        }

        public async Task NotifyConversationStarted()
        {
            if (!_agentState.IsInitialized || _conversationCTS.IsCancellationRequested)
            {
                _logger.LogWarning("Agent {AgentId}: Cannot notify start - not initialized or shutting down.", AgentId);
                return;
            }

            bool requiresLanguageSelection = _agentState.CurrentSessionContext?.Language.MultiLanguageEnabled == true &&
                                            _agentState.CurrentSessionContext.Language.EnabledMultiLanguages?.Count > 1;

            if (requiresLanguageSelection)
            {
                // todo make this delay configurable by the user
                await Task.Delay(1000);
                await SetupLanguageSelectionViaDTMFAsync(_conversationCTS.Token);
            }
            else
            {
                await BeginAgentConversationFlowAsync();
            }
        }

        private async Task SetupLanguageSelectionViaDTMFAsync(CancellationToken cancellationToken)
        {
            try
            {
                await HandleLanguageSelectionMessagePlaying(cancellationToken);

                var dtmfConfig = new DTMFSessionConfig
                {
                    AssociatedNodeId = "internal::language_selection",
                    MaxLength = 1,
                    TerminatorChar = null,
                    InterDigitTimeoutSeconds = 5,
                    MaxSessionDurationSeconds = 30
                };

                bool started = _dtmfSessionManager.StartSession(dtmfConfig);
                if (started)
                {
                    _agentState.IsAwaitingLanguageSelectionInput = true;
                }
                else
                {
                    _logger.LogError("Agent {AgentId}: Failed to start DTMF session for language selection (already active?).", _agentState.AgentId);
                    // Fallback? Maybe proceed with default language?
                    await BeginAgentConversationFlowAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {AgentId}: Error during language selection setup. Proceeding with default.", _agentState.AgentId);
                await BeginAgentConversationFlowAsync();
            }
        }

        private string cachedMultiLanguagePlayMessage = string.Empty;
        private async Task HandleLanguageSelectionMessagePlaying(CancellationToken cancellationToken)
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

            await _audioOutputHandler.SynthesizeAndPlayBlockingAsync(cachedMultiLanguagePlayMessage, cancellationToken);
        }

        private async Task BeginAgentConversationFlowAsync()
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
                    _logger.LogDebug("Agent {AgentId}: Playing agent-first opening message.", AgentId);
                    foreach (var dynamicVariable in _agentState.CurrentSessionContext.DynamicVariables ?? new Dictionary<string, string>())
                    {
                        openingMessage = openingMessage.Replace(("{{" + dynamicVariable.Key + "}}"), dynamicVariable.Value);
                    }

                    // Format for LLM history, synthesize raw text
                    string llmHistoryMessage = "response_to_customer: " + openingMessage;
                    _agentState.LLMService?.AddAssistantMessage(llmHistoryMessage); // Add to history
                    AgentTextResponse?.Invoke(this, new ConversationTextGeneratedEventArgs(llmHistoryMessage, _agentState.CurrentClientId ?? "Start", false)); // Raw text event
                    await _audioOutputHandler.SynthesizeAndPlayBlockingAsync(openingMessage, _conversationCTS.Token);
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
            _agentState.IsAcceptingSTTAudio = true;
            // TODO: Enable VAD if needed (_vadService.Start() ?)

            _logger.LogInformation("Agent {AgentId}: Ready for interaction.", AgentId);
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
                await _interruptionManager.ReInitializeForLanguageAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {AgentId}: Failed to re-initialize modules for language {LanguageCode}", AgentId, newLanguageCode);
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs($"Failed to switch language: {ex.Message}", ex));
                // What state are we in now? Might need to shut down. TODO, this should not happen tho
                await ShutdownAsync("Language change failed");
            }
        }

        public async Task NotifyMaxDurationReached()
        {
            if (!_agentState.IsInitialized) return;
            _logger.LogWarning("Agent {AgentId}: Maximum conversation duration reached.", AgentId);

            // Stop accepting new input
            _agentState.IsAcceptingSTTAudio = false;
            // TODO: Maybe stop VAD processing too?

            // Cancel any ongoing agent response forcefully
            await _llmHandler.CancelCurrentLLMTaskAsync();
            await _audioOutputHandler.CancelCurrentSpeechPlaybackAsync();

            // Instruct LLM to end the call immediately
            string endCallCommand = $"Maximum duration of {_agentState.CurrentSessionContext?.Timeout.MaxCallTimeS ?? 0} seconds reached perform end_call with reason and notifying customer why right away.";      
            await _llmHandler.ProcessSystemMessageAsync(endCallCommand, _agentState.CurrentClientId, _conversationCTS.Token); // todo this is problematic
        }

        public Task ProcessAudioAsync(byte[] audioData, string clientId, CancellationToken cancellationToken)
        {
            if (!_agentState.IsInitialized || _conversationCTS.IsCancellationRequested) return Task.CompletedTask;

            // Set client ID (important for multi-participant, maybe less so for 1-1)
            // todo dont know what this is used for, might be problematic if turn by turn is not enabled?
            _agentState.CurrentClientId = clientId;

            // Pass to input handler
            _audioInputHandler.ProcessAudioChunk(audioData, cancellationToken);
            return Task.CompletedTask;
        }

        private async Task ProcessTranscriptionResultAsync(string text)
        {
            if (!_agentState.IsInitialized || _conversationCTS.IsCancellationRequested) return;
            _logger.LogDebug("Agent {AgentId}: Orchestrator received transcription: '{Text}'", AgentId, text);

            _agentState.IsSTTRecognizing = false;

            try
            {
                bool canAgentContinue = await _interruptionManager.CheckShouldLetAgentBeInterrupted(text, _agentState.CurrentClientId, _conversationCTS.Token);
                if (canAgentContinue)
                {
                    _logger.LogInformation("Agent {AgentId}: Text handled by interruption manager, leting agent to continue sppeaking.", AgentId);
                    return;
                }

                string modifiedText = "";
                if (_agentState.InterruptResponseBuffer.Length > 0)
                {
                    modifiedText += _agentState.InterruptResponseBuffer.ToString();
                    _agentState.InterruptResponseBuffer.Clear();
                }
                if (!string.IsNullOrEmpty(_llmHandler.CurrentlyProcessingMessage))
                {
                    if (modifiedText.Length > 0) modifiedText += " ";
                    modifiedText += _llmHandler.CurrentlyProcessingMessage;
                }
                if (modifiedText.Length > 0) modifiedText += " ";
                modifiedText += text;

                await _llmHandler.ProcessUserTextAsync(modifiedText, _agentState.CurrentClientId, _conversationCTS.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Agent {AgentId}: Text processing cancelled.", AgentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {AgentId}: Error processing text input: {Text}", AgentId, text);
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs($"Error processing text: {ex.Message}", ex));
            }
        }

        public Task ProcessTextAsync(string text, string? clientId, CancellationToken cancellationToken)
        {
            if (clientId != null)
            {
                _agentState.CurrentClientId = clientId;
            }
            return ProcessTranscriptionResultAsync(text);
        }

        public Task ProcessDTMFAsync(string digit, string? clientId, CancellationToken cancellationToken)
        {
            if (!_agentState.IsInitialized || _conversationCTS.IsCancellationRequested) return Task.CompletedTask;

            if (clientId != null) _agentState.CurrentClientId = clientId;

            // Pass digit to the session manager
            _dtmfSessionManager.ProcessDigit(digit);

            // No need for language selection logic here anymore, it's handled by OnDtmfSessionEnded

            return Task.CompletedTask;
        }
        private async void OnDtmfSessionEnded(object? sender, DTMFSessionEventArgs args)
        {
            _logger.LogInformation("Agent {AgentId}: DTMF session for Node {NodeId} ended. Reason: {Reason}, Digits: '{Digits}'",
                _agentState.AgentId, args.NodeId, args.Reason, args.CollectedDigits);

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
                        await _audioOutputHandler.SynthesizeAndPlayBlockingAsync("Invalid Language Selection.", CancellationToken.None);
                        await HandleLanguageSelectionMessagePlaying(CancellationToken.None);
                        _dtmfSessionManager.ResumeSession();
                        return;
                    }
                }
                else if (args.Reason == DTMFSessionEndReason.TimeoutInterDigit)
                {
                    await _audioOutputHandler.SynthesizeAndPlayBlockingAsync("Please choose a language.", CancellationToken.None);
                    await HandleLanguageSelectionMessagePlaying(CancellationToken.None);
                    _dtmfSessionManager.ResumeSession();
                    return;
                }

                // TODO ask user how much to wait for after language selection
                // or if they wish for something to be played first
                // could move it to BeginAgentConversationFlowAsync as well after background audio?
                await Task.Delay(2000);

                _agentState.IsAwaitingLanguageSelectionInput = false;
                await BeginAgentConversationFlowAsync();
                _dtmfSessionManager.CancelSession("Language Selected");
                return;
            }
            // --- Handle Regular DTMF Tool Results ---
            else
            {
                // Package the result and send it back to the LLM as a system message
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
                _logger.LogDebug("Agent {AgentId}: Sending DTMF result to LLM: {ResultMessage}", _agentState.AgentId, resultMessage);
                await _llmHandler.ProcessSystemMessageAsync(resultMessage, args.ClientId, CancellationToken.None); // Use CancellationToken.None? Or master token?
            }
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

            await (_llmHandler?.CancelCurrentLLMTaskAsync() ?? Task.CompletedTask);
            _llmHandler?.Dispose();

            _sttHandler?.StopTranscription();
            _sttHandler?.Dispose();

            _interruptionManager?.Dispose();

            _dtmfSessionManager?.Dispose();

            _audioOutputHandler?.StopSending();
            _audioOutputHandler?.Dispose();

            // Dispose Tool Executor? (If it holds resources) TODO > might need to cancel current httpclient ongoign request and dispose
            // _toolExecutor?.Dispose(); // If it implements IDisposable

            UnwireEvents();

            _conversationCTS?.Dispose();

            _logger.LogInformation("AI Agent {AgentId} shut down complete.", AgentId);
        }

        private void UnwireEvents()
        {
            AudioGenerated = null;
            AgentTextResponse = null;
            ClientTextQuery = null;
            ClearBufferedAudio = null;
            Thinking = null;
            ErrorOccurred = null;
        }

        public void Dispose()
        {
            _logger.LogDebug("Disposing ConversationAIAgent {AgentId}.", AgentId);

            if (_agentState.IsInitialized || !_conversationCTS.IsCancellationRequested)
            {
                ShutdownAsync("Agent Disposed").Wait(TimeSpan.FromSeconds(5)); // Blocking wait on dispose is risky, use timeout todo why?
            }

            _logger.LogDebug("ConversationAIAgent {AgentId} disposed.", AgentId);
        }
    }
}