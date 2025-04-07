using IqraCore.Entities.Business;
using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Interfaces.AI; // For ILLMService (needed?)
using IqraCore.Interfaces.Conversation;
using IqraCore.Interfaces.VAD; // For IVadService (needed?)
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Languages;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.LLM.Providers.Helpers;
using IqraInfrastructure.Managers.Script;
using IqraInfrastructure.Managers.STT;
using IqraInfrastructure.Managers.TTS;
using IqraInfrastructure.Managers.VAD;
using IqraInfrastructure.Repositories.Business;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic; // For List
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IqraInfrastructure.Managers.Conversation.Modules; // Import the modules namespace

namespace IqraInfrastructure.Managers.Conversation
{
    public class ConversationAIAgent : IConversationAgent, IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ConversationAIAgent> _logger;

        // Original Dependencies (Managers, etc.)
        private readonly ConversationSessionManager _conversationSessionManager;
        private readonly BusinessManager _businessManager;
        private readonly SystemPromptGenerator _systemPromptGenerator;
        private readonly LanguagesManager _langaugesManager;
        private readonly STTProviderManager _sttProviderManager;
        private readonly TTSProviderManager _ttsProviderManager;
        private readonly LLMProviderManager _llmProviderManager;
        private readonly ScriptExecutionManager _scriptExecutionManager; // ToolExecutor needs this
        private readonly BusinessAgentAudioRepository _audioRepository; // AudioOutput needs this

        // Agent State & Modules
        private readonly ConversationAIAgentState _agentState;
        private readonly ConversationAIAgentAudioInput _audioInputHandler;
        private readonly ConversationAIAgentSTTHandler _sttHandler;
        private readonly ConversationAIAgentLLMHandler _llmHandler;
        private readonly ConversationAIAgentToolExecutor _toolExecutor;
        private readonly ConversationAIAgentAudioOutput _audioOutputHandler;
        private readonly ConversationAIAgentInterruptionManager _interruptionManager;
        private readonly ConversationAIAgentDTMFHandler _dtmfHandler;

        // Master Cancellation Token
        private CancellationTokenSource _conversationCTS = new(); // Initialize here

        // --- Public Interface ---
        public string AgentId => _agentState.AgentId;
        public ConversationAgentType AgentType => ConversationAgentType.AI;

        // Events
        public event EventHandler<ConversationAudioGeneratedEventArgs>? AudioGenerated;
        public event EventHandler<ConversationTextGeneratedEventArgs>? TextGenerated;
        public event EventHandler<ConversationAgentThinkingEventArgs>? Thinking; // TODO: Wire this up if needed
        public event EventHandler<ConversationAgentErrorEventArgs>? ErrorOccurred;


        public ConversationAIAgent(
            ILoggerFactory loggerFactory,
            ConversationSessionManager sessionManager,
            string agentId, // Agent ID passed in
            BusinessManager businessManager,
            SystemPromptGenerator systemPromptGenerator,
            STTProviderManager sttProviderManager,
            TTSProviderManager ttsProviderManager,
            LLMProviderManager llmProviderManager,
            LanguagesManager languagesManager,
            BusinessAgentAudioRepository audioRepository
            // ScriptExecutionManager is likely a Singleton or Scoped service, inject if needed
            // Or create it here if it makes sense:
            // ScriptExecutionManager scriptExecutionManager
            )
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<ConversationAIAgent>();

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
            _agentState = new ConversationAIAgentState(agentId);

            // Create ScriptExecutionManager (or inject)
            _scriptExecutionManager = new ScriptExecutionManager(
                _loggerFactory.CreateLogger<ScriptExecutionManager>()
            );

            // --- Instantiate Modules ---
            // Pass logger factory, state, and necessary managers/module refs
            _audioOutputHandler = new ConversationAIAgentAudioOutput(_loggerFactory, _agentState, _ttsProviderManager, _audioRepository, _businessManager);
            _llmHandler = new ConversationAIAgentLLMHandler(_loggerFactory, _agentState, _llmProviderManager, _businessManager, _systemPromptGenerator);
            _toolExecutor = new ConversationAIAgentToolExecutor(_loggerFactory, _agentState, _scriptExecutionManager /*, _conversationSessionManager */); // Pass session manager if needed, or use event
            _audioInputHandler = new ConversationAIAgentAudioInput(_loggerFactory, _agentState);
            _sttHandler = new ConversationAIAgentSTTHandler(_loggerFactory, _agentState, _sttProviderManager, _businessManager);
            _interruptionManager = new ConversationAIAgentInterruptionManager(_loggerFactory, _agentState, _llmProviderManager, _businessManager, _audioOutputHandler, _llmHandler);
            _dtmfHandler = new ConversationAIAgentDTMFHandler(_loggerFactory, _agentState, _langaugesManager, _audioOutputHandler, HandleLanguageChangeRequestAsync); // Pass callback


            // --- Wire up Events between Modules and Orchestrator ---
            WireUpEvents();

            _logger.LogInformation("ConversationAIAgent {AgentId} instance created.", agentId);
        }

        private void WireUpEvents()
        {
            // Audio Output -> Orchestrator (Public Events)
            _audioOutputHandler.AudioChunkGenerated += (sender, args) => AudioGenerated?.Invoke(this, args);
            _audioOutputHandler.SpeechPlaybackComplete += OnSpeechPlaybackComplete; // Handle completion signal

            // STT Handler -> Orchestrator (Process Text)
            _sttHandler.TranscriptionReceived += ProcessTranscriptionResultAsync;

            // LLM Handler -> Orchestrator/Other Modules
            _llmHandler.SynthesizeTextRequested += (text) => _audioOutputHandler.SynthesizeAndQueueSpeechAsync(text, CancellationToken.None); // Use appropriate token
            _llmHandler.TextChunkGenerated += (text) => TextGenerated?.Invoke(this, new ConversationTextGeneratedEventArgs(text, _agentState.CurrentClientId));
            _llmHandler.ResponseHandlingComplete += OnLLMResponseHandlingComplete; // May not be needed if SpeechPlaybackComplete is used
            _llmHandler.SystemToolExecutionRequested += (content) => _toolExecutor.HandleSystemToolAsync(content, _conversationCTS.Token); // Use agent token
            _llmHandler.CustomToolExecutionRequested += (content) => _toolExecutor.HandleCustomToolAsync(content, _conversationCTS.Token); // Use agent token

            // Tool Executor -> Orchestrator/Other Modules
            _toolExecutor.ToolResultAvailable += (result) => _llmHandler.ProcessSystemMessageAsync(result, CancellationToken.None); // Feed result back to LLM
            _toolExecutor.PlaySpeechRequested += (text, token) => _audioOutputHandler.SynthesizeAndPlayBlockingAsync(text, token); // Request blocking speech
            _toolExecutor.EndConversationRequested += (reason) => {
                _logger.LogInformation("Agent {AgentId} requested conversation end via tool. Reason: {Reason}", AgentId, reason);
                // Use the injected session manager to end
                return _conversationSessionManager.EndAsync(reason);
            };

            // TODO: Add Error Event Handling from Modules
            // Example: _llmHandler.ErrorOccurred += HandleModuleError;
        }

        private void HandleModuleError(object? sender, ConversationAgentErrorEventArgs args)
        {
            _logger.LogError(args.Exception, "Agent {AgentId}: Error occurred in module {Module}: {Message}", AgentId, sender?.GetType().Name ?? "Unknown", args.Message);
            ErrorOccurred?.Invoke(this, args); // Bubble up the error
            // Decide on further action - shutdown? Attempt recovery?
        }

        private void OnSpeechPlaybackComplete()
        {
            // This is signaled by AudioOutput when the speech queue is empty and the last segment finished playing.
            _logger.LogDebug("Agent {AgentId}: Received signal that speech playback is complete.", AgentId);
            // If LLM was waiting for speech, it can now proceed (if necessary)
            // This helps decouple LLM completion from actual audio finishing.
        }

        private void OnLLMResponseHandlingComplete()
        {
            // This is signaled by LLMHandler when it finishes processing a 'response_to_customer' stream.
            _logger.LogDebug("Agent {AgentId}: Received signal that LLM response handling is complete.", AgentId);
            // Usually followed by waiting for OnSpeechPlaybackComplete.
        }

        public async Task InitializeAsync(ConversationAgentConfiguration config, BusinessApp businessAppData, BusinessAppRoute businessRouteData, CancellationToken cancellationToken)
        {
            if (_agentState.IsInitialized)
            {
                _logger.LogWarning("AI Agent {AgentId} is already initialized.", AgentId);
                return;
            }
            _logger.LogInformation("AI Agent {AgentId} initializing...", AgentId);

            // Link the external cancellation token with the agent's master CTS
            _conversationCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _agentState.AgentConfiguration = config; // Provide master token via config? No, state doesn't own it.

            try
            {
                // --- Populate Initial State ---
                _agentState.BusinessApp = businessAppData;
                _agentState.CurrentSessionRoute = businessRouteData;
                _agentState.CurrentConversationType = businessRouteData.Agent.ConversationType;
                _agentState.CurrentLanguageCode = businessRouteData.Language.DefaultLanguageCode; // Initial language
                _agentState.BusinessAppAgent = await _businessManager.GetAgentsManager().GetAgentById(config.BusinessId, businessRouteData.Agent.SelectedAgentId);
                if (_agentState.BusinessAppAgent == null)
                {
                    throw new InvalidOperationException($"Business app agent {businessRouteData.Agent.SelectedAgentId} not found");
                }
                _agentState.BackgroundMusicVolume = _agentState.BusinessAppAgent.Settings?.BackgroundAudioVolume ?? 0.3f; // Get from config


                // --- Initialize Modules (in dependency order if needed) ---
                // State is populated, now init handlers that rely on it.
                // Order: Base services (LLM/TTS/STT might be needed by others), then Input/Output/Interruption/Tools.
                await _llmHandler.InitializeAsync();
                await _audioOutputHandler.InitializeAsync(_conversationCTS.Token);
                await _sttHandler.InitializeAsync();
                await _toolExecutor.InitializeAsync(_conversationCTS.Token); // Load scripts
                await _interruptionManager.InitializeAsync(_conversationCTS.Token);
                await _dtmfHandler.InitializeAsync();
                await _audioInputHandler.InitializeAsync(_conversationCTS.Token); // Start processing queue last

                _agentState.IsInitialized = true;
                _logger.LogInformation("AI Agent {AgentId} initialized successfully. Route: {RouteId}, Lang: {Lang}, Type: {Type}",
                    AgentId, config.RouteId, _agentState.CurrentLanguageCode, _agentState.CurrentConversationType);
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
            if (!_agentState.IsInitialized)
            {
                _logger.LogWarning("Agent {AgentId}: Cannot notify start - not initialized.", AgentId);
                return;
            }
            _logger.LogInformation("Agent {AgentId}: Conversation started notification received.", AgentId);

            // Handle multi-language selection via DTMF handler
            await _dtmfHandler.SetupLanguageSelectionAsync(_conversationCTS.Token);

            // If not awaiting language selection, begin the conversation flow
            if (!_dtmfHandler.IsAwaitingLanguageSelection) // Need public property/method on DTMF handler
            {
                await BeginAgentConversationFlowAsync();
            }
            // If awaiting language, BeginAgentConversationFlowAsync will be called after language selection
        }

        // Called after language selection (if any) or directly from NotifyConversationStarted
        private async Task BeginAgentConversationFlowAsync()
        {
            _logger.LogInformation("Agent {AgentId}: Beginning main conversation flow.", AgentId);
            if (_agentState.BusinessAppAgent?.Utterances.OpeningType == BusinessAppAgentOpeningType.AgentFirst)
            {
                string openingMessage = _agentState.BusinessAppAgent.Utterances.GreetingMessage[_agentState.CurrentLanguageCode];
                _logger.LogDebug("Agent {AgentId}: Playing opening greeting: {Message}", AgentId, openingMessage);
                // Add to LLM history *before* speaking?
                _agentState.LLMService?.AddAssistantMessage(openingMessage);
                await _audioOutputHandler.SynthesizeAndPlayBlockingAsync(openingMessage, _conversationCTS.Token);
            }
            else // User first or other types
            {
                _logger.LogDebug("Agent {AgentId}: Waiting for user to speak first.", AgentId);
                // Acknowledge call start in LLM history silently?
                _agentState.LLMService?.AddAssistantMessage("execute_system_function: acknowledge(\"Call Start - User First\")");
            }

            _agentState.IsAcceptingSTTAudio = true; // Start processing audio for STT
                                                    // VAD is already initialized and listening via InterruptionManager if needed
            _logger.LogInformation("Agent {AgentId}: Ready for interaction.", AgentId);
        }

        // Callback for DTMF handler when language changes
        private async Task HandleLanguageChangeRequestAsync(string newLanguageCode)
        {
            _logger.LogInformation("Agent {AgentId}: Handling language change request to {LanguageCode}", AgentId, newLanguageCode);
            if (!_agentState.IsInitialized) return; // Should not happen

            // 1. Update State
            _agentState.CurrentLanguageCode = newLanguageCode;

            // 2. Cancel ongoing operations? Optional, but safer.
            await _llmHandler.CancelCurrentLLMTaskAsync();
            await _audioOutputHandler.CancelCurrentSpeechPlaybackAsync();

            // 3. Re-initialize language-dependent modules
            try
            {
                _logger.LogDebug("Agent {AgentId}: Re-initializing language-dependent modules.", AgentId);
                // Order matters if prompts depend on each other
                await _llmHandler.ReInitializeForLanguageAsync();
                await _sttHandler.ReInitializeForLanguageAsync();
                await _audioOutputHandler.ReInitializeForLanguageAsync(_conversationCTS.Token);
                await _toolExecutor.ReInitializeForLanguageAsync(_conversationCTS.Token); // Reload scripts if language-specific
                await _interruptionManager.ReInitializeForLanguageAsync(_conversationCTS.Token); // Re-init interrupting LLM if needed

                _logger.LogInformation("Agent {AgentId}: Language successfully changed to {LanguageCode}.", AgentId, newLanguageCode);

                // 4. After re-initialization, start the conversation flow
                // Maybe play a confirmation in the new language?
                // await _audioOutputHandler.SynthesizeAndPlayBlockingAsync($"Language set to {newLanguageCode}.", _conversationCTS.Token); // Example

                await BeginAgentConversationFlowAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {AgentId}: Failed to re-initialize modules for language {LanguageCode}", AgentId, newLanguageCode);
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs($"Failed to switch language: {ex.Message}", ex));
                // What state are we in now? Might need to shut down.
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
            string reasonMessage = $"Maximum duration of {_agentState.CurrentSessionRoute?.Configuration.MaxCallTimeS ?? 0} seconds reached.";
            string endCallCommand = $"Perform execute_system_function: end_call: \"{reasonMessage}\", \"{reasonMessage} The call will now end.\""; // Example command

            _logger.LogInformation("Agent {AgentId}: Instructing LLM to end call due to max duration.", AgentId);
            // Use ProcessSystemMessageAsync or similar to inject this command
            await _llmHandler.ProcessSystemMessageAsync(endCallCommand, CancellationToken.None); // Use None or agent token?

            // The end_call tool execution should handle the final steps.
        }

        public Task ProcessAudioAsync(byte[] audioData, string clientId, CancellationToken cancellationToken)
        {
            if (!_agentState.IsInitialized || _conversationCTS.IsCancellationRequested) return Task.CompletedTask;

            // Set client ID (important for multi-participant, maybe less so for 1-1)
            _agentState.CurrentClientId = clientId;

            // Pass to input handler
            _audioInputHandler.ProcessAudioChunk(audioData, cancellationToken);
            return Task.CompletedTask;
        }

        // Triggered by STTHandler TranscriptionReceived event
        private async Task ProcessTranscriptionResultAsync(string text)
        {
            if (!_agentState.IsInitialized || _conversationCTS.IsCancellationRequested) return;
            _logger.LogDebug("Agent {AgentId}: Orchestrator received transcription: '{Text}'", AgentId, text);

            try
            {
                // If agent is responding, check for interruption via InterruptionManager
                if (_agentState.IsResponding || _agentState.IsExecutingSystemTool || _agentState.IsExecutingCustomTool) // Also check tools? Maybe not. Primarily check IsResponding.
                {
                    _logger.LogTrace("Agent {AgentId}: Checking for potential interruption due to incoming text.", AgentId);
                    bool handledByInterrupt = await _interruptionManager.HandlePotentialInterruptionAsync(text, _agentState.CurrentClientId, _conversationCTS.Token);
                    if (handledByInterrupt)
                    {
                        _logger.LogDebug("Agent {AgentId}: Text handled by interruption manager.", AgentId);
                        return; // Interruption logic took over
                    }
                    else
                    {
                        // Interruption logic decided not to handle it (e.g., turn-by-turn, or AI decided continue)
                        _logger.LogDebug("Agent {AgentId}: Text not handled by interruption manager, proceeding.", AgentId);
                        // Fall through to process normally IF interruption manager didn't buffer/handle it
                    }
                }

                // If not interrupting or interruption passed, process text via LLM handler
                await _llmHandler.ProcessUserTextAsync(text, _agentState.CurrentClientId, _conversationCTS.Token);

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

        // Interface method - delegates to ProcessTranscriptionResultAsync (which handles interruption)
        public Task ProcessTextAsync(string text, string? clientId, CancellationToken cancellationToken) // Made clientId nullable
        {
            // This external entry point might be called directly sometimes.
            // Route it through the same logic as STT results.
            // Update client ID if provided.
            if (clientId != null)
            {
                _agentState.CurrentClientId = clientId;
            }
            return ProcessTranscriptionResultAsync(text);
        }


        public Task ProcessDTMFAsync(string digit, string? clientId, CancellationToken cancellationToken) // Made clientId nullable
        {
            if (!_agentState.IsInitialized || _conversationCTS.IsCancellationRequested) return Task.CompletedTask;

            if (clientId != null)
            { // Update context if needed
                _agentState.CurrentClientId = clientId;
            }

            // Pass to DTMF handler
            return _dtmfHandler.ProcessDigitAsync(digit, cancellationToken);
        }


        public async Task ShutdownAsync(string reason)
        {
            if (!_agentState.IsInitialized && _conversationCTS.IsCancellationRequested) // Check if already shutting down/shut down
            {
                _logger.LogInformation("AI Agent {AgentId} shutdown already in progress or completed.", AgentId);
                return;
            }
            _logger.LogInformation("AI Agent {AgentId} shutting down. Reason: {Reason}", AgentId, reason);

            // Signal shutdown start
            _agentState.IsInitialized = false; // Mark as not initialized first
            _agentState.IsAcceptingSTTAudio = false;
            if (!_conversationCTS.IsCancellationRequested)
            {
                _conversationCTS.Cancel(); // Trigger cancellation for all linked tokens
            }


            // --- Stop/Dispose Modules (Reverse order of Initialization?) ---
            // Stop input processing first to prevent new work
            _audioInputHandler?.Dispose(); // Dispose should stop the task

            // Stop STT - explicit stop might be good before dispose
            _sttHandler?.StopTranscription();
            _sttHandler?.Dispose();

            // Stop Interruption Manager (VAD timer etc)
            _interruptionManager?.Dispose(); // Dispose should cancel timers/tasks

            // Stop DTMF handling? (DTMF handler might not have long running tasks)
            _dtmfHandler?.Reset(); // Reset state if needed

            // Stop LLM processing and Audio Output last, as they might be finishing final words
            await (_llmHandler?.CancelCurrentLLMTaskAsync() ?? Task.CompletedTask); // Ensure LLM task cancelled
            _llmHandler?.Dispose();

            _audioOutputHandler?.StopSending(); // Signal sending loop to stop gracefully
            _audioOutputHandler?.Dispose(); // Dispose waits for loop internally

            // Dispose Tool Executor? (If it holds resources)
            // _toolExecutor?.Dispose(); // If it implements IDisposable

            // Unwire events to prevent leaks (optional if agent is disposed)
            UnwireEvents();

            // Dispose the master CTS
            _conversationCTS?.Dispose();


            _logger.LogInformation("AI Agent {AgentId} shut down complete.", AgentId);
        }

        private void UnwireEvents()
        {
            // Manually set events to null or unsubscribe if WireUpEvents stored delegates
            AudioGenerated = null;
            TextGenerated = null;
            Thinking = null;
            ErrorOccurred = null;

            // TODO: Unsubscribe handlers within modules if WireUpEvents used +=
            // This requires storing the handler delegates or careful +=/-= management
            // Example (conceptual):
            // if (_sttHandler != null) _sttHandler.TranscriptionReceived -= ProcessTranscriptionResultAsync;
            // ... and so on for all subscriptions made in WireUpEvents ...
            // It's often simpler to rely on the modules being disposed and garbage collected if the Orchestrator is also disposed.
        }

        public void Dispose()
        {
            _logger.LogDebug("Disposing ConversationAIAgent {AgentId}.", AgentId);
            // Ensure shutdown is called if not already
            if (_agentState.IsInitialized || !_conversationCTS.IsCancellationRequested)
            {
                ShutdownAsync("Agent Disposed").Wait(TimeSpan.FromSeconds(5)); // Blocking wait on dispose is risky, use timeout
            }
            else
            {
                // Already shutting down or shut down, ensure CTS is disposed
                _conversationCTS?.Dispose();
            }

            // Dispose managed resources owned directly by Orchestrator if any
            _scriptExecutionManager?.Dispose(); // If it's disposable and created here
            _logger.LogDebug("ConversationAIAgent {AgentId} disposed.", AgentId);
        }
    }
}