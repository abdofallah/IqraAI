using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Agent;
using IqraCore.Interfaces.VAD;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.VAD;
using IqraInfrastructure.Managers.VAD.Silero;
using Microsoft.Extensions.Logging;
using System.Text;


namespace IqraInfrastructure.Managers.Conversation.Session.Agent.AI
{
    // The new, more powerful manager class
    public class TurnAndInterruptionManager : IDisposable
    {
        // --- NEW EVENTS ---
        // Fired when the manager has confidently determined the user's turn is over.
        // The string parameter will contain the final, consolidated text of the turn.
        public event Func<string, Task>? UserTurnEnded;

        // Fired when the agent should temporarily stop speaking.
        public event Func<Task>? AgentShouldPause;

        // Fired when the agent, having been paused, should resume speaking.
        public event Func<Task>? AgentShouldResume;

        // Fired when an interruption is verified and the agent should fully stop its current thought.
        // The string will contain the user's interrupting text.
        public event Func<string, Task>? VerifiedInterruptionOccurred;


        // Dependencies (similar to before, but with a clearer purpose)
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<TurnAndInterruptionManager> _logger;
        private readonly ConversationAIAgentState _agentState;
        private readonly BusinessAppAgentInterruption _config; // Store the specific config for easy access

        // Internal state for tracking turns
        private bool _isUserTurn = false;
        private readonly StringBuilder _userTurnTextBuffer = new StringBuilder();

        // Internal state for pause trigger
        private int _currentTurnWordCount = 0;

        // --- NEW: VAD State Trackers for different purposes ---
        private VadStateTracker? _turnEndVadTracker;
        private VadStateTracker? _pauseTriggerVadTracker;

        // --- NEW STATE FLAGS FOR RACE CONDITION HANDLING ---
        private bool _vadHasIndicatedTurnEnd = false;
        private bool _sttHasProvidedFinalTranscript = false;
        private bool _isUserTurnActive = false; // To know when to start looking for turn end signals

        public TurnAndInterruptionManager(
            ILoggerFactory loggerFactory,
            ConversationAIAgentState agentState,
            LLMProviderManager llmProviderManager, // Still needed for AI turn-end/verification
            BusinessManager businessManager       // Still needed for AI turn-end/verification
        )
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<TurnAndInterruptionManager>();
            _agentState = agentState;
            // A crucial step: get a direct reference to the configuration.
            _config = _agentState.BusinessAppAgent.Interruptions;
        }

        public async Task InitializeAsync(CancellationToken agentCTS)
        {
            _logger.LogInformation("Turn and Interruption Manager initializing with TurnEnd strategy: {TurnEndType}", _config.TurnEnd.Type);

            // --- Setup Turn-End VAD Tracker ---
            if (_config.TurnEnd.Type == AgentInterruptionTurnEndTypeENUM.VAD)
            {
                _logger.LogDebug("Configuring VAD State Tracker for Turn End detection.");
                var options = new VadTrackerOptions
                {
                    // Using a default threshold, can be made configurable later
                    Threshold = 0.5f,
                    // This is the key setting for turn-end: how long of a silence means the user is done?
                    MinSilenceDurationMs = _config.TurnEnd.VadSilenceDurationMS ?? 1200,
                    // We require a minimum amount of speech to consider it a turn
                    MinSpeechDurationMs = 150
                };
                _turnEndVadTracker = new VadStateTracker(options);
                _turnEndVadTracker.SpeechEnded += OnTurnEndVadSpeechEnded;
                // Subscribe to the core's probability updates
                _agentState.SileroVadCore.SpeechProbabilityUpdated += _turnEndVadTracker.ProcessProbability;
            }

            // --- Setup Pause-Trigger VAD Tracker ---
            if (!_config.UseTurnByTurnMode && _config.PauseTrigger?.Type == AgentInterruptionPauseTriggerTypeENUM.VAD)
            {
                _logger.LogDebug("Configuring VAD State Tracker for Pause Trigger detection.");
                var options = new VadTrackerOptions
                {
                    Threshold = 0.5f,
                    MinSilenceDurationMs = 10000, // Not relevant for this tracker's purpose
                                                  // This is the key setting: how little speech from the user should trigger a pause?
                    MinSpeechDurationMs = _config.PauseTrigger.VadDurationMS ?? 200
                };
                _pauseTriggerVadTracker = new VadStateTracker(options);
                _pauseTriggerVadTracker.SpeechStarted += OnPauseTriggerVadSpeechStarted;
                // Subscribe to the same core's updates
                _agentState.SileroVadCore.SpeechProbabilityUpdated += _pauseTriggerVadTracker.ProcessProbability;
            }
        }

        // --- NEW TEXT ENTRY POINT ---
        // This will be called by STTHandler for partial and final results.
        public void ProcessTranscriptionForTurnAnalysis(string text, bool isFinal)
        {
            // A new turn begins the moment we get any transcript.
            if (!_isUserTurnActive)
            {
                _isUserTurnActive = true;
                // Reset flags for the new turn
                _vadHasIndicatedTurnEnd = false;
                _sttHasProvidedFinalTranscript = false;
            }

            // --- REFINED LOGIC: Overwrite buffer, don't append partials ---
            if (!isFinal)
            {
                _userTurnTextBuffer.Append(text);
            }
            else
            {
                _userTurnTextBuffer.Clear().Append(text);
            }

            // --- Handle Pause Trigger Logic (unchanged) ---
            // We might want to refine this to not use the whole buffer text, but the new chunk.
            // For now, let's keep it simple.
            HandlePauseTrigger(text);

            if (isFinal)
            {
                _logger.LogDebug("STT reported final transcript.");
                _sttHasProvidedFinalTranscript = true;

                // Check if we can conclude the turn.
                TryConcludeUserTurn();
            }
        }

        private void HandlePauseTrigger(string newTextChunk)
        {
            // Only trigger a pause if interruptible mode is on AND the agent is currently speaking.
            if (_config.UseTurnByTurnMode || !_agentState.IsResponding)
            {
                return;
            }

            if (_config.PauseTrigger?.Type == AgentInterruptionPauseTriggerTypeENUM.STT)
            {
                var wordCountThreshold = _config.PauseTrigger.WordCount;
                if (wordCountThreshold.HasValue && wordCountThreshold > 0)
                {
                    // Simple word count logic.
                    _currentTurnWordCount += newTextChunk.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;

                    if (_currentTurnWordCount >= wordCountThreshold.Value)
                    {
                        _logger.LogInformation("STT Pause Trigger: Word count ({Current}) reached/exceeded threshold ({Threshold}). Firing AgentShouldPause.", _currentTurnWordCount, wordCountThreshold.Value);
                        AgentShouldPause?.Invoke();

                        // We need to prevent this from firing repeatedly in the same turn.
                        // A simple way is to set the threshold to an unreachable value after it's triggered once.
                        _config.PauseTrigger.WordCount = int.MaxValue; // This is a temporary state change for this session only.
                    }
                }
            }
        }

        // --- NEW METHOD FOR DIRECT TEXT INPUT ---
        public async Task ProcessDirectTextInputAsync(string text, CancellationToken cancellationToken)
        {
            // Here, we can add logic to handle interruption if the agent is busy.
            // For now, we will assume a direct text input should always interrupt.
            if (_agentState.IsResponding || _agentState.IsExecutingCustomTool || _agentState.IsExecutingSystemTool) // Simplified check
            {
                _logger.LogInformation("Direct text input received while agent was busy. Firing verified interruption.");

                // This is a future-proof step. Instead of just ending the turn, we signal a full interruption.
                // This will tell the agent to cancel its current LLM/Audio tasks.
                if (VerifiedInterruptionOccurred != null)
                {
                    await VerifiedInterruptionOccurred.Invoke(text);
                }
                return;
            }

            // If the agent is not busy, this is a normal turn.
            _logger.LogDebug("Processing direct text input as a complete user turn.");

            // Clear any lingering state from a previous audio-based turn.
            _userTurnTextBuffer.Clear();
            _currentTurnWordCount = 0;

            // Fire the main event to trigger the agent's brain.
            if (UserTurnEnded != null)
            {
                await UserTurnEnded.Invoke(text);
            }
        }

        // --- NEW VAD EVENT HANDLERS ---
        private void OnTurnEndVadSpeechEnded()
        {
            if (_isUserTurnActive)
            {
                _logger.LogDebug("VAD Turn End Tracker detected speech end.");
                _vadHasIndicatedTurnEnd = true;

                // Check if we can conclude the turn.
                TryConcludeUserTurn();
            }
        }

        // The new central decision-making method
        private void TryConcludeUserTurn()
        {
            bool canConclude = false;

            // Apply the logic based on the configured Turn End strategy
            switch (_config.TurnEnd.Type)
            {
                case AgentInterruptionTurnEndTypeENUM.STT:
                    // For STT strategy, we only need the final transcript. VAD is irrelevant.
                    if (_sttHasProvidedFinalTranscript)
                    {
                        canConclude = true;
                    }
                    break;

                case AgentInterruptionTurnEndTypeENUM.VAD:
                    // For VAD strategy, we need BOTH VAD silence AND a final transcript.
                    if (_vadHasIndicatedTurnEnd && _sttHasProvidedFinalTranscript)
                    {
                        canConclude = true;
                    }
                    break;

                    // Future strategies will have their own conditions here.
                    // case AgentInterruptionTurnEndTypeENUM.AI:
                    // case AgentInterruptionTurnEndTypeENUM.ML:
                    //     break;
            }

            if (canConclude)
            {
                _logger.LogInformation("Conditions met to conclude user turn.");
                var finalText = _userTurnTextBuffer.ToString().Trim();

                // --- FIRE THE EVENT AND RESET STATE ---
                UserTurnEnded?.Invoke(finalText);

                // Reset all state for the next turn.
                _userTurnTextBuffer.Clear();
                _currentTurnWordCount = 0;
                _isUserTurnActive = false;
                _vadHasIndicatedTurnEnd = false;
                _sttHasProvidedFinalTranscript = false;
                _turnEndVadTracker?.Reset();
            }
        }

        private void OnPauseTriggerVadSpeechStarted()
        {
            // This event fires when the tracker, using its "short speech" config,
            // detects that the user has started talking.

            // We only care if the agent is currently speaking.
            if (_agentState.IsResponding)
            {
                _logger.LogInformation("VAD Pause Trigger Tracker detected user speech while agent was responding. Firing AgentShouldPause.");
                AgentShouldPause?.Invoke();

                // We should "disarm" this tracker for the rest of the agent's speech turn
                // to avoid it firing constantly. We can do this by unsubscribing.
                // Note: This requires access to the vadCore. We need to store it.
                // Let's refine this in the next step. For now, the concept is key.
            }
        }

        public void Dispose()
        {
            
        }
    }
}