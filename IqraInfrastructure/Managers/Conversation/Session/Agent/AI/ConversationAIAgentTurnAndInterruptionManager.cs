using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Agent;
using IqraCore.Interfaces.AI;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.TurnEnd;
using IqraInfrastructure.Managers.VAD;
using Microsoft.Extensions.Logging;
using System.Text;

namespace IqraInfrastructure.Managers.Conversation.Session.Agent.AI
{
    public class ConversationAIAgentTurnAndInterruptionManager : IDisposable
    {
        public event Func<string, Task>? UserTurnEnded;
        public event Func<Task>? AgentShouldPause;
        public event Func<Task>? AgentShouldResume;
        public event Func<string, Task>? VerifiedInterruptionOccurred;

        private readonly ILogger<ConversationAIAgentTurnAndInterruptionManager> _logger;
        private readonly ConversationAIAgentState _agentState;
        private readonly LLMProviderManager _llmProviderManager;
        private readonly BusinessManager _businessManager;


        private BusinessAppAgentInterruption _config;

        // VAD State Trackers
        private VadStateTracker? _turnEndVadTracker;
        private VadStateTracker? _pauseTriggerVadTracker;

        // Turn State
        private readonly StringBuilder _userTurnTextBuffer = new StringBuilder();
        private int _currentTurnWordCount = 0;
        private bool _isUserTurnActive = false;
        private bool _isAgentPaused = false;

        // Synchronization Flags for Turn End
        private bool _vadHasIndicatedTurnEnd = false;
        private bool _sttHasProvidedFinalTranscript = false;

        // AI Turn-End Components
        private ILLMService? _turnEndLLMService;
        private Task? _turnEndLLMTask;
        private CancellationTokenSource _turnEndLLMCTS = new CancellationTokenSource();
        private bool _aiHasIndicatedTurnEnd = false;

        private SmartTurnService? _mlTurnService;
        private bool _mlHasIndicatedTurnEnd = false;

        public ConversationAIAgentTurnAndInterruptionManager(
            ILoggerFactory loggerFactory,
            ConversationAIAgentState agentState,
            LLMProviderManager llmProviderManager,
            BusinessManager businessManager
        ) {
            _logger = loggerFactory.CreateLogger<ConversationAIAgentTurnAndInterruptionManager>();
            _agentState = agentState;
            _llmProviderManager = llmProviderManager;
            _businessManager = businessManager;
        }

        // Initialize
        public async Task InitializeAsync(CancellationToken agentCTS)
        {
            _config = _agentState.BusinessAppAgent.Interruptions;

            _logger.LogInformation("Turn and Interruption Manager initializing with TurnEnd strategy: {TurnEndType}", _config.TurnEnd.Type);

            if (_agentState.SileroVadCore == null)
            {
                _logger.LogWarning("SileroVadCore is not available in agent state. VAD-based features will be disabled.");
                return;
            }

            // Setup VAD tracker for Turn End detection if configured
            if (_config.TurnEnd.Type == AgentInterruptionTurnEndTypeENUM.VAD)
            {
                _logger.LogDebug("Configuring VAD State Tracker for Turn End detection.");
                var options = new VadTrackerOptions
                {
                    Threshold = 0.5f, // This could be made configurable in the future
                    MinSilenceDurationMs = _config.TurnEnd.VadSilenceDurationMS ?? 1200,
                    MinSpeechDurationMs = 150 // A user must speak for at least 150ms to constitute a turn
                };
                _turnEndVadTracker = new VadStateTracker(options);
                _turnEndVadTracker.SpeechEnded += OnTurnEndVadSpeechEnded;
                _agentState.SileroVadCore.SpeechProbabilityUpdated += _turnEndVadTracker.ProcessProbability;
            }

            // Setup VAD tracker for Pause Trigger detection if configured
            if (!_config.UseTurnByTurnMode && _config.PauseTrigger?.Type == AgentInterruptionPauseTriggerTypeENUM.VAD)
            {
                _logger.LogDebug("Configuring VAD State Tracker for Pause Trigger detection.");
                var options = new VadTrackerOptions
                {
                    Threshold = 0.5f,
                    MinSilenceDurationMs = 30000, // Irrelevant for this tracker, set to a high value
                    MinSpeechDurationMs = _config.PauseTrigger.VadDurationMS ?? 200 // How little speech triggers a pause
                };
                _pauseTriggerVadTracker = new VadStateTracker(options);
                _pauseTriggerVadTracker.SpeechStarted += OnPauseTriggerVadSpeechStarted;
                _agentState.SileroVadCore.SpeechProbabilityUpdated += _pauseTriggerVadTracker.ProcessProbability;
            }

            _turnEndLLMCTS = CancellationTokenSource.CreateLinkedTokenSource(agentCTS);
            // Setup AI Turn-End LLM
            if (_config.TurnEnd.Type == AgentInterruptionTurnEndTypeENUM.AI)
            {
                await InitializeTurnEndLLMAsync();
            }

            if (_config.TurnEnd.Type == AgentInterruptionTurnEndTypeENUM.ML)
            {
                _mlTurnService = new SmartTurnService();
                _mlTurnService.TurnEnded += OnMlTurnEnded;
            }
        }
        private async Task InitializeTurnEndLLMAsync()
        {
            _logger.LogDebug("Configuring LLM for AI Turn End detection.");

            var llmConfig = _config.TurnEnd.LLMIntegration;
            if (llmConfig == null)
            {
                _logger.LogError("AI Turn End is configured, but no LLM integration was provided. Falling back to STT.");
                _config.TurnEnd.Type = AgentInterruptionTurnEndTypeENUM.STT; // Graceful fallback
                return;
            }

            var integrationDataResult = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(_agentState.BusinessApp.Id, llmConfig.Id);
            if (!integrationDataResult.Success || integrationDataResult.Data == null)
            {
                _logger.LogError("Could not find business integration with ID {IntegrationId} for AI Turn End. Falling back to STT.", llmConfig.Id);
                _config.TurnEnd.Type = AgentInterruptionTurnEndTypeENUM.STT;
                return;
            }

            var llmResult = await _llmProviderManager.BuildProviderServiceByIntegration(integrationDataResult.Data, llmConfig, new Dictionary<string, string>());
            if (!llmResult.Success || llmResult.Data == null)
            {
                _logger.LogError("Failed to build LLM service for AI Turn End: {Message}. Falling back to STT.", llmResult.Message);
                _config.TurnEnd.Type = AgentInterruptionTurnEndTypeENUM.STT;
                return;
            }

            _turnEndLLMService = llmResult.Data;

            // This system prompt is CRITICAL for both accuracy and low latency.
            // It's designed to force a simple, quick response.
            _turnEndLLMService.SetSystemPrompt("You are an expert at analyzing real-time speech transcripts. Your task is to determine if a user has finished their turn. Analyze the user's utterance. Respond with ONLY ONE of the following words: 'CONTINUE' if the user is likely still speaking or has paused mid-sentence. 'END' if the user has likely finished their complete thought or question.");
        }

        // Management
        public void ProcessTranscriptionForTurnAnalysis(string text, bool isFinal)
        {
            if (!_isUserTurnActive)
            {
                _isUserTurnActive = true;
                // Reset synchronization flags for the new turn
                _vadHasIndicatedTurnEnd = false;
                _sttHasProvidedFinalTranscript = false;
            }

            // Overwrite the buffer with the most recent (and complete) transcript version.
            if (isFinal)
            {
                _userTurnTextBuffer.Clear().Append(text);
            }
            else
            {
                // For partial results, we might append or replace based on provider behavior.
                // For now, replacing is safer to avoid duplication.
                _userTurnTextBuffer.Clear().Append(text);
            }

            HandlePauseTrigger(text);

            // Trigger AI Turn-End Check
            if (_config.TurnEnd.Type == AgentInterruptionTurnEndTypeENUM.AI)
            {
                // We get the most up-to-date text from our buffer
                var currentUtterance = _userTurnTextBuffer.ToString();
                // We run this in the background (fire-and-forget) to not block anything.
                _ = CheckAITurnEndAsync(currentUtterance);
            }

            if (isFinal)
            {
                _logger.LogDebug("STT reported final transcript.");
                _sttHasProvidedFinalTranscript = true;
                TryConcludeUserTurn();
            }
        }
        public async Task ProcessDirectTextInputAsync(string text, CancellationToken cancellationToken)
        {
            // If the agent is busy, a direct text input is always treated as a verified interruption.
            if (_agentState.IsResponding || _agentState.IsExecutingCustomTool || _agentState.IsExecutingSystemTool)
            {
                _logger.LogInformation("Direct text input received while agent was busy. Firing VerifiedInterruptionOccurred.");
                VerifiedInterruptionOccurred?.Invoke(text);
                return;
            }

            // If the agent is idle, this is a normal turn.
            _logger.LogDebug("Processing direct text input as a complete user turn.");
            UserTurnEnded?.Invoke(text);
        }
        public void NotifyAgentSpeechCompleted()
        {
            _isAgentPaused = false;
            _pauseTriggerVadTracker?.Reset();
        }

        // Event Handlers
        private void HandlePauseTrigger(string newTextChunk)
        {
            if (_config.UseTurnByTurnMode || !_agentState.IsResponding || _isAgentPaused)
            {
                return; // Not applicable or already paused
            }

            if (_config.PauseTrigger?.Type == AgentInterruptionPauseTriggerTypeENUM.STT)
            {
                var wordCountThreshold = _config.PauseTrigger.WordCount ?? 0;
                if (wordCountThreshold > 0)
                {
                    _currentTurnWordCount += newTextChunk.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;

                    if (_currentTurnWordCount >= wordCountThreshold)
                    {
                        _logger.LogInformation("STT Pause Trigger: Word count ({Current}) met threshold ({Threshold}). Firing AgentShouldPause.", _currentTurnWordCount, wordCountThreshold);
                        AgentShouldPause?.Invoke();
                        _isAgentPaused = true;
                    }
                }
            }
        }
        private void OnPauseTriggerVadSpeechStarted()
        {
            if (_agentState.IsResponding && !_isAgentPaused)
            {
                _logger.LogInformation("VAD Pause Trigger detected user speech while agent was responding. Firing AgentShouldPause.");
                AgentShouldPause?.Invoke();
                _isAgentPaused = true;
            }
        }
        private void OnTurnEndVadSpeechEnded()
        {
            if (_isUserTurnActive)
            {
                _logger.LogDebug("VAD Turn End Tracker detected speech end.");
                _vadHasIndicatedTurnEnd = true;
                TryConcludeUserTurn();
            }
        }
        private void OnMlTurnEnded()
        {
            _logger.LogDebug("ML Model detected turn end.");
            _mlHasIndicatedTurnEnd = true;
            TryConcludeUserTurn();
        }
        private void TryConcludeUserTurn()
        {
            bool canConclude = false;

            switch (_config.TurnEnd.Type)
            {
                case AgentInterruptionTurnEndTypeENUM.STT:
                    canConclude = _sttHasProvidedFinalTranscript;
                    break;
                case AgentInterruptionTurnEndTypeENUM.VAD:
                    canConclude = _vadHasIndicatedTurnEnd && _sttHasProvidedFinalTranscript;
                    break;
                case AgentInterruptionTurnEndTypeENUM.AI:
                    canConclude = _aiHasIndicatedTurnEnd && _sttHasProvidedFinalTranscript;
                    break;
                case AgentInterruptionTurnEndTypeENUM.ML:
                    canConclude = _mlHasIndicatedTurnEnd && _sttHasProvidedFinalTranscript;
                    break;
            }

            if (!canConclude)
            {
                return; // Conditions not yet met.
            }

            var finalText = _userTurnTextBuffer.ToString().Trim();

            if (_isAgentPaused)
            {
                // This was an interruption. We now decide if it's valid.
                bool shouldInterrupt = true; // Default to interrupting
                if (_config.Verification != null && _config.Verification.Enabled)
                {
                    _logger.LogWarning("Verification LLM not yet implemented. Defaulting to interruption.");
                    // In the future, this is where the verification logic would go:
                    // shouldInterrupt = await CheckVerificationAsync(finalText);
                }

                if (shouldInterrupt)
                {
                    VerifiedInterruptionOccurred?.Invoke(finalText);
                }
                else
                {
                    AgentShouldResume?.Invoke();
                }
            }
            else
            {
                // This was a normal, uninterrupted turn.
                UserTurnEnded?.Invoke(finalText);
            }

            // A conclusion was reached, so reset state for the next turn.
            ResetTurnState();
        }
        private void ResetTurnState()
        {
            _userTurnTextBuffer.Clear();
            _currentTurnWordCount = 0;
            _isUserTurnActive = false;
            _isAgentPaused = false;
            _vadHasIndicatedTurnEnd = false;
            _sttHasProvidedFinalTranscript = false;
            _turnEndVadTracker?.Reset();
            _pauseTriggerVadTracker?.Reset();
            _aiHasIndicatedTurnEnd = false;
            _mlHasIndicatedTurnEnd = false;
        }
        private async Task CheckAITurnEndAsync(string currentUtterance)
        {
            if (_turnEndLLMService == null || string.IsNullOrWhiteSpace(currentUtterance)) return;

            // Cancel any previous, now-outdated check
            if (_turnEndLLMTask != null && !_turnEndLLMTask.IsCompleted)
            {
                _turnEndLLMCTS.Cancel();
                _turnEndLLMCTS = CancellationTokenSource.CreateLinkedTokenSource(_agentState.MasterCancellationToken);
            }

            try
            {
                //var llmResponse = await _turnEndLLMService.ProcessSingleInputAsync(
                //    $"Utterance: \"{currentUtterance}\"", _turnEndLLMCTS.Token);

                //if (llmResponse.Success)
                //{
                //    var result = llmResponse.Data?.Trim().ToUpper();
                //    _logger.LogTrace("AI Turn End check returned: {Result}", result);
                //    if (result == "END")
                //    {
                //        _aiHasIndicatedTurnEnd = true;
                //        TryConcludeUserTurn();
                //    }
                //}
            }
            catch (OperationCanceledException)
            {
                _logger.LogTrace("AI turn end check was cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during AI turn end check.");
            }
        }

        // Disposal
        public void Dispose()
        {
            if (_agentState.SileroVadCore != null)
            {
                if (_turnEndVadTracker != null) _agentState.SileroVadCore.SpeechProbabilityUpdated -= _turnEndVadTracker.ProcessProbability;
                if (_pauseTriggerVadTracker != null) _agentState.SileroVadCore.SpeechProbabilityUpdated -= _pauseTriggerVadTracker.ProcessProbability;
            }
            _turnEndLLMCTS?.Cancel();
            (_turnEndLLMService as IDisposable)?.Dispose();
            _mlTurnService?.Dispose();
        }
    }
}