using IqraCore.Entities.Business;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Helper.Agent;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.AI;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.LLM.Providers.Helpers;
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

        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ConversationAIAgentTurnAndInterruptionManager> _logger;
        private readonly ConversationAIAgentLLMHandler _agentLLMHandler;
        private readonly ConversationAIAgentAudioOutput _agentAudioOutput;
        private readonly ConversationAIAgentState _agentState;
        private readonly LLMProviderManager _llmProviderManager;
        private readonly BusinessManager _businessManager;

        private BusinessAppAgentInterruption _config;

        // VAD State Trackers
        private VadStateTracker? _turnEndVadTracker;
        private VadStateTracker? _pauseTriggerVadTracker;

        // Turn State
        private readonly StringBuilder _userTurnTextBuffer = new StringBuilder();
        private readonly StringBuilder _userTurnTextFinalBuffer = new StringBuilder();
        private int _currentTurnWordCount = 0;
        private bool _isUserTurnActive = false;
        private bool _isAgentPaused = false;

        // Synchronization Flags for Turn End
        private bool _vadHasIndicatedTurnEnd = false;
        private bool _sttHasProvidedFinalTranscript = false;

        // AI Turn-End Components
        private ILLMService? _turnEndLLMService;
        private StringBuilder _turnEndLLMInputBuffer = new StringBuilder();
        private Task? _turnEndLLMTask;
        private CancellationTokenSource _turnEndLLMCTS = new CancellationTokenSource();
        private bool _aiHasIndicatedTurnEnd = false;

        // Turn End ML Components
        private readonly List<byte> _userTurnAudioBuffer = new List<byte>();
        private SmartTurnService? _mlTurnService;
        private bool _mlHasIndicatedTurnEnd = false;
        private VadStateTracker _mlTurnVadTracker;

        // AI Interruption Verification Componenets
        private ILLMService? _interruptionVerificationLLMService;
        private StringBuilder _interruptionVerificationLLMInputBuffer = new StringBuilder();
        private Task? _interruptionVerificationLLMTask;
        private CancellationTokenSource _interruptionVerificationLLMCTS = new CancellationTokenSource();
        private bool _isAwaitingVerification = false;
        private bool _hasVerifiedInterruptionResult = false;
        private bool _canInterruptAgentAfterVerificaiton = false;

        public ConversationAIAgentTurnAndInterruptionManager(
            ILoggerFactory loggerFactory,
            ConversationAIAgentLLMHandler agentLLMHandler,
            ConversationAIAgentAudioOutput agentAudioOutput,
            ConversationAIAgentState agentState,
            LLMProviderManager llmProviderManager,
            BusinessManager businessManager
        ) {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<ConversationAIAgentTurnAndInterruptionManager>();

            _agentLLMHandler = agentLLMHandler;
            _agentAudioOutput = agentAudioOutput;
            _agentState = agentState;
            _llmProviderManager = llmProviderManager;
            _businessManager = businessManager;
        }

        // Initialize
        public async Task InitializeAsync(CancellationToken agentCTS)
        {
            _config = _agentState.BusinessAppAgent.Interruptions;

            if (_agentState.SileroVadCore == null)
            {
                _logger.LogWarning("SileroVadCore is not available in agent state. VAD-based features will be disabled.");
                return;
            }

            // Setup VAD tracker for Turn End detection if configured
            if (_config.TurnEnd.Type == AgentInterruptionTurnEndTypeENUM.VAD)
            {
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
                var options = new VadTrackerOptions
                {
                    Threshold = 0.5f,
                    MinSilenceDurationMs = 30000, // Irrelevant for this tracker, set to a high value
                    MinSpeechDurationMs = _config.PauseTrigger.VadDurationMS!.Value
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
                _logger.LogDebug("Configuring SmartTurnService for ML Turn End detection.");
                _mlTurnService = new SmartTurnService(_loggerFactory, _agentState);
                _mlTurnService.TurnEnded += OnMlTurnEnded;

                var options = new VadTrackerOptions
                {
                    Threshold = 0.5f,
                    MinSilenceDurationMs = 300,
                    MinSpeechDurationMs = 150 // A user must speak for at least 150ms to constitute a turn
                };
                _mlTurnVadTracker = new VadStateTracker(options);
                _mlTurnVadTracker.SpeechEnded += TriggerMlAnalysis;
                _agentState.SileroVadCore.SpeechProbabilityUpdated += _mlTurnVadTracker.ProcessProbability;
            }

            if (!_config.UseTurnByTurnMode && _config.Verification!.Enabled)
            {
                await InitializeInterruptionVerificaitonLLMAsync();
            }
        }
        private async Task InitializeTurnEndLLMAsync()
        {
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
            _turnEndLLMService.SetSystemPrompt("You are an expert at analyzing real-time speech transcripts. Your task is to determine if a user has finished their turn. Analyze the user's utterance. Respond with ONLY ONE of the following words: 'CONTINUE' if the user is likely still speaking or has paused mid-sentence. 'END' if the user has likely finished their complete thought or question.");
            _turnEndLLMService.MessageStreamed += OnAITurnEndLLMMessageStream;
        }
        private async Task InitializeInterruptionVerificaitonLLMAsync()
        {
            var llmConfig = _config.Verification!.LLMIntegration;
            if (llmConfig == null)
            {
                _logger.LogError("AI Interruption Verification is configured, but no LLM integration was provided. Falling back to disabling verification.");
                _config.Verification.Enabled = false; // Graceful fallback
                return;
            }

            var integrationDataResult = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(_agentState.BusinessApp.Id, llmConfig.Id);
            if (!integrationDataResult.Success || integrationDataResult.Data == null)
            {
                _logger.LogError("Could not find business integration with ID {IntegrationId} for AI Interruption Verification. Falling back to disabling verification.", llmConfig.Id);
                _config.Verification.Enabled = false;
                return;
            }

            var llmResult = await _llmProviderManager.BuildProviderServiceByIntegration(integrationDataResult.Data, llmConfig, new Dictionary<string, string>());
            if (!llmResult.Success || llmResult.Data == null)
            {
                _logger.LogError("Failed to build LLM service for AI Turn End: {Message}. Falling back to disabling verification.", llmResult.Message);
                _config.Verification.Enabled = false;
                return;
            }

            _interruptionVerificationLLMService = llmResult.Data;
            _interruptionVerificationLLMService.SetSystemPrompt("You are an expert at analyzing real-time speech transcripts. Your task is to determine if a user speech should interrupt the agent speech. Analyze the user's utterance. Respond with ONLY ONE of the following words: 'CONTINUE' if the user speech should not interrupt the agent. 'INTERRUPT' if the user has likely interrupted the agent.");
            _interruptionVerificationLLMService.MessageStreamed += OnAIInterruptionVerificationMessageStream;
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
                _aiHasIndicatedTurnEnd = false;
                _mlHasIndicatedTurnEnd = false;
            }

            // Overwrite the buffer with the most recent (and complete) transcript version.
            if (isFinal)
            {
                _userTurnTextFinalBuffer.Append(text);
            }
            else
            {
                // For partial results, we might append or replace based on provider behavior.
                // For now, replacing is safer to avoid duplication.
                _userTurnTextBuffer.Append(text);
            }


            var currentFinalUtterance = _userTurnTextFinalBuffer.ToString();
            var currentUtterance = _userTurnTextBuffer.ToString();

            // Pause Trigger
            if (isFinal)
            {
                HandlePauseTrigger(currentFinalUtterance);
            }
            else
            {
                HandlePauseTrigger(currentUtterance);
            }

            // Turn End Checks
            if (isFinal)
            {
                // Trigger AI Turn-End Check
                if (_config.TurnEnd.Type == AgentInterruptionTurnEndTypeENUM.AI)
                {
                    _ = CheckAITurnEndAsync(currentFinalUtterance);
                }

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
        public void BufferAudioForMlAnalysis(byte[] audioData)
        {
            // Only buffer if the ML service is active and a user turn has started.
            if (_mlTurnService != null && _isUserTurnActive)
            {
                _userTurnAudioBuffer.AddRange(audioData);
            }
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
                // If in standard VAD mode, set the flag.
                if (_config.TurnEnd.Type == AgentInterruptionTurnEndTypeENUM.VAD)
                {
                    _logger.LogDebug("VAD Turn End Tracker detected speech end.");
                    _vadHasIndicatedTurnEnd = true;
                    TryConcludeUserTurn();
                }
                // If in ML mode, VAD acts as a trigger, not a conclusion.
                // The call to TriggerMlAnalysis is handled by the event subscription in InitializeAsync.
            }
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

            var finalText = _userTurnTextFinalBuffer.ToString().Trim();

            if (!_isAgentPaused)
            {
                UserTurnEnded?.Invoke(finalText);
            }
            else
            {
                if (_config.Verification!.Enabled)
                {
                    if (_isAwaitingVerification)
                    {
                        if (_hasVerifiedInterruptionResult)
                        {
                            if (_canInterruptAgentAfterVerificaiton)
                            {
                                VerifiedInterruptionOccurred?.Invoke(_userTurnTextFinalBuffer.ToString());
                            }
                            else
                            {
                                AgentShouldResume?.Invoke();
                            }
                        }
                        else
                        {
                            // wait for ai to give us the result back
                            // do nothing
                            return;
                        }
                    }
                    else
                    {
                        _isAwaitingVerification = true;
                        _hasVerifiedInterruptionResult = false;
                        _canInterruptAgentAfterVerificaiton = false;

                        _ = CheckAIInterruptionVerificationAsync(_userTurnTextFinalBuffer.ToString());

                        return;
                    }
                }
                else
                {
                    // how did we reach here?
                    AgentShouldResume?.Invoke();
                }
            }

            // A conclusion was reached, so reset state for the next turn.
            ResetTurnState();
        }
        private void ResetTurnState()
        {
            _userTurnAudioBuffer.Clear();
            _userTurnTextBuffer.Clear();
            _userTurnTextFinalBuffer.Clear();
            _currentTurnWordCount = 0;
            _isUserTurnActive = false;
            _isAgentPaused = false;
            _vadHasIndicatedTurnEnd = false;
            _sttHasProvidedFinalTranscript = false;
            _turnEndVadTracker?.Reset();
            _pauseTriggerVadTracker?.Reset();
            _aiHasIndicatedTurnEnd = false;
            _mlHasIndicatedTurnEnd = false;
            _isAwaitingVerification = false;
            _canInterruptAgentAfterVerificaiton = false;
            _hasVerifiedInterruptionResult = false;
        }
        // AI Turn End
        private async Task CheckAITurnEndAsync(string currentUtterance)
        {
            if (_turnEndLLMService == null || string.IsNullOrWhiteSpace(currentUtterance)) return;

            // Cancel any previous, now-outdated check
            if (_turnEndLLMTask != null && !_turnEndLLMTask.IsCompleted)
            {
                _turnEndLLMCTS.Cancel();
                _turnEndLLMCTS = CancellationTokenSource.CreateLinkedTokenSource(_agentState.MasterCancellationToken);
            }
            if (_turnEndLLMInputBuffer.Length > 0) _turnEndLLMInputBuffer.Clear();

            try
            {
                _turnEndLLMService.ClearMessages();

                var requestText = $"Agent Previous Utterance: \"{_agentLLMHandler.GetCurrentResponseText()}\"\n\nUser Utterance: \"{currentUtterance}\"";

                _logger.LogInformation(requestText); // todo debug remove

                _turnEndLLMService.AddUserMessage(requestText);
                _turnEndLLMTask = _turnEndLLMService.ProcessInputAsync(_turnEndLLMCTS.Token);
            }
            catch (OperationCanceledException){ /* Expected */ }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during AI turn end check.");
            }
        }
        private void OnAITurnEndLLMMessageStream(object? sender, ConversationAgentEventLLMStreamed? data)
        {
            FunctionReturnResult<(string? deltaText, bool isEndOfResponse)?> result = LLMStreamingChunkDataExtractHelper.GetChunkData(data.ResponseObject, _turnEndLLMService.GetProviderType());

            if (!result.Success || !result.Data.HasValue)
            {
                _logger.LogError("Agent {AgentId}: Error extracting AI turn end LLM chunk, {Reason}", _agentState.AgentId, result.Message);
                return;
            }

            if (!string.IsNullOrEmpty(result.Data.Value.deltaText))
            {
                _turnEndLLMInputBuffer.Append(result.Data.Value.deltaText);
            }
            if (result.Data.Value.isEndOfResponse)
            {
                var bufferString = _turnEndLLMInputBuffer.ToString();

                string trimmedText = bufferString.Replace("\n", " ").Replace("\r", " ").Replace("\t", " ").Trim();
                int lastSpaceIndex = trimmedText.LastIndexOf(' ');
                string lastWord;

                if (lastSpaceIndex == -1) // No spaces, the whole string is the word
                {
                    lastWord = trimmedText;
                }
                else
                {
                    lastWord = trimmedText.Substring(lastSpaceIndex + 1);
                }

                if (lastWord == "END")
                {
                    _aiHasIndicatedTurnEnd = true;
                    TryConcludeUserTurn();
                }
            }      
        }
        // AI Interuption Verification
        private async Task CheckAIInterruptionVerificationAsync(string currentUtterance)
        {
            if (_interruptionVerificationLLMService == null || string.IsNullOrWhiteSpace(currentUtterance)) return;

            // Cancel any previous, now-outdated check
            if (_interruptionVerificationLLMTask != null && !_interruptionVerificationLLMTask.IsCompleted)
            {
                _interruptionVerificationLLMCTS.Cancel();
                _interruptionVerificationLLMCTS = CancellationTokenSource.CreateLinkedTokenSource(_agentState.MasterCancellationToken);
            }
            if (_interruptionVerificationLLMInputBuffer.Length > 0) _interruptionVerificationLLMInputBuffer.Clear();

            try
            {
                _interruptionVerificationLLMService.ClearMessages();

                // todo here we should isntead of llm handler, get the spoken text from the audio output text segments

                var requestText = $"Agent Previous Utterance: \"{_agentLLMHandler.GetCurrentResponseText()}\"\n\nUser Utterance: \"{currentUtterance}\"";

                _logger.LogInformation(requestText); // todo debug remove

                _interruptionVerificationLLMService.AddUserMessage(requestText);
                _interruptionVerificationLLMTask = _interruptionVerificationLLMService.ProcessInputAsync(_interruptionVerificationLLMCTS.Token);
            }
            catch (OperationCanceledException) { /* Expected */ }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during AI interruption verification check.");
            }
        }
        private void OnAIInterruptionVerificationMessageStream(object? sender, ConversationAgentEventLLMStreamed? data)
        {
            FunctionReturnResult<(string? deltaText, bool isEndOfResponse)?> result = LLMStreamingChunkDataExtractHelper.GetChunkData(data.ResponseObject, _interruptionVerificationLLMService.GetProviderType());

            if (!result.Success || !result.Data.HasValue)
            {
                _logger.LogError("Error extracting interruption verification LLM chunk: {Reason}", result.Message);
                return;
            }

            if (!string.IsNullOrEmpty(result.Data.Value.deltaText))
            {
                _interruptionVerificationLLMInputBuffer.Append(result.Data.Value.deltaText);
            }

            if (result.Data.Value.isEndOfResponse)
            {
                var bufferString = _interruptionVerificationLLMInputBuffer.ToString();

                string trimmedText = bufferString.Replace("\n", " ").Replace("\r", " ").Replace("\t", " ").Trim();
                int lastSpaceIndex = trimmedText.LastIndexOf(' ');
                string lastWord;

                if (lastSpaceIndex == -1) // No spaces, the whole string is the word
                {
                    lastWord = trimmedText;
                }
                else
                {
                    lastWord = trimmedText.Substring(lastSpaceIndex + 1);
                }

                _hasVerifiedInterruptionResult = true;
                if (lastWord == "INTERRUPT")
                {
                    _canInterruptAgentAfterVerificaiton = true;
                }
                TryConcludeUserTurn();
            }
        }
        // ML Turn End
        private void TriggerMlAnalysis()
        {
            if (_mlTurnService != null && _userTurnAudioBuffer.Count > 0)
            {
                _logger.LogDebug("VAD silence triggered ML turn analysis on buffered audio.");
                var turnAudio = _userTurnAudioBuffer.ToArray();
                _userTurnAudioBuffer.Clear(); // Clear the buffer after copying
                _mlTurnService.AnalyzeTurn(turnAudio);
            }
        }
        private void OnMlTurnEnded()
        {
            _logger.LogDebug("ML Model detected turn end.");
            _mlHasIndicatedTurnEnd = true;
            TryConcludeUserTurn();
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
            _mlTurnService?.Dispose();
        }
    }
}