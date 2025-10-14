using IqraCore.Entities.Business;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Conversation.Turn;
using IqraCore.Entities.Helper.Agent;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.AI;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Conversation.Session.Logger;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.LLM.Providers.Helpers;
using IqraInfrastructure.Managers.TurnEnd;
using IqraInfrastructure.Managers.VAD;
using Microsoft.CognitiveServices.Speech.Transcription;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Core.Misc;
using NAudio.SoundFont;
using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IqraInfrastructure.Managers.Conversation.Session.Agent.AI
{
    public class ConversationAIAgentTurnAndInterruptionManager : IDisposable
    {
        public event Func<ConversationTurn, Task>? NewTurnCreated;
        public event Func<ConversationTurn, Task>? UserTurnFinalized;
        public event Func<ConversationTurn, Task>? VerifiedInterruptionOccurred;
        public event Func<Task>? AgentShouldPause;
        public event Func<Task>? AgentShouldResume;

        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ConversationAIAgentTurnAndInterruptionManager> _logger;
        private readonly ConversationAIAgentLLMHandler _agentLLMHandler;
        private readonly ConversationAIAgentAudioOutput _agentAudioOutput;
        private readonly ConversationAIAgentState _agentState;
        private readonly LLMProviderManager _llmProviderManager;
        private readonly BusinessManager _businessManager;

        private BusinessAppAgentInterruption _config;

        // Semaphore for sequential stt processing
        private readonly SemaphoreSlim _transcriptionProcessingLock = new SemaphoreSlim(1, 1);

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
        private CancellationTokenSource? _mlTurnFallbackCts;

        // AI Interruption Verification Componenets
        private ILLMService? _interruptionVerificationLLMService;
        private StringBuilder _interruptionVerificationLLMInputBuffer = new StringBuilder();
        private Task? _interruptionVerificationLLMTask;
        private CancellationTokenSource _interruptionVerificationLLMCTS = new CancellationTokenSource();
        private bool _isAwaitingVerification = false;
        private bool _hasVerifiedInterruptionResult = false;
        private bool _canInterruptAgentAfterVerificaiton = false;

        public ConversationAIAgentTurnAndInterruptionManager(
            SessionLoggerFactory loggerFactory,
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
                    MinSilenceDurationMs = _config.TurnEnd.VadSilenceDurationMS.Value,
                    MinSpeechDurationMs = _config.TurnEnd.VadSpeechDurationMS.Value
                };
                _turnEndVadTracker = new VadStateTracker(options);
                _turnEndVadTracker.SpeechStarted += async (duration) =>
                {
                    await OnTurnEndVadSpeechStarted();
                };
                _turnEndVadTracker.SpeechEnded += async (duration) => {
                    await OnTurnEndVadSpeechEnded();
                };
                _agentState.SileroVadCore.SpeechProbabilityUpdated += _turnEndVadTracker.ProcessProbability;
            }

            // Setup VAD tracker for Pause Trigger detection if configured
            if (!_config.UseTurnByTurnMode && _config.PauseTrigger?.Type == AgentInterruptionPauseTriggerTypeENUM.VAD)
            {
                var options = new VadTrackerOptions
                {
                    Threshold = 0.5f,
                    MinSilenceDurationMs = 300, // Irrelevant for this tracker, set to a high value
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
                _mlTurnService = new SmartTurnService(
                    _loggerFactory.CreateLogger<SmartTurnService>(),
                    _agentState.AgentConfiguration.AudioEncodingType,
                    _agentState.AgentConfiguration.SampleRate,
                    _agentState.AgentConfiguration.BitsPerSample
                );
                _mlTurnService.TurnEnded += async () => { await OnMlTurnEnded(); };

                var options = new VadTrackerOptions
                {
                    Threshold = 0.5f,
                    MinSilenceDurationMs = _config.TurnEnd.MLTurnEndVADMinimumSilenceDurationMS.Value,
                    MinSpeechDurationMs = _config.TurnEnd.MLTurnEndVADMinimumSpeechDurationMS.Value
                };
                _mlTurnVadTracker = new VadStateTracker(options);
                _mlTurnVadTracker.SpeechStarted += async (duration) => {
                    await OnMLTurnVadSpeechStarted();
                };
                _mlTurnVadTracker.SpeechEnded += async (duration) => {
                    await OnMLTurnVadSpeechEnded();
                };
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

            var llmResult = await _llmProviderManager.BuildProviderServiceByIntegration(_loggerFactory, integrationDataResult.Data, llmConfig, new Dictionary<string, string>());
            if (!llmResult.Success || llmResult.Data == null)
            {
                _logger.LogError("Failed to build LLM service for AI Turn End: {Message}. Falling back to STT.", llmResult.Message);
                _config.TurnEnd.Type = AgentInterruptionTurnEndTypeENUM.STT;
                return;
            }

            _turnEndLLMService = llmResult.Data;
            _turnEndLLMService.SetSystemPrompt("You are a highly advanced AI specializing in real-time conversational turn-taking analysis. Your primary function is to analyze a user's speech transcript and determine with high accuracy whether they have finished their turn. Your analysis must be nuanced, considering both grammatical structure and conversational context.\r\n\r\n**OUTPUT INSTRUCTIONS**\r\n\r\nYour response MUST STRICTLY follow the format below.\r\n1.  **Explanation:** First, provide a brief, one-sentence explanation of your reasoning.\r\n2.  **Result Label:** On a new line, write the literal string `Result:`.\r\n3.  **JSON Object:** Immediately following the label, provide a code block containing a single, well-formed JSON object.\r\n\r\n**JSON Object Structure:**\r\n*   `\"Classification\"`: (String) Must be either \"END\" or \"CONTINUE\".\r\n*   `\"Reasoning\"`: (String) A concise, machine-readable explanation for the classification (e.g., \"User asked a complete question\", \"User is dictating an email address\").\r\n*   `\"WaitBeforeConsideringEndMilliseconds\"`: (Integer) This key MUST be included ONLY if the `\"Classification\"` is \"CONTINUE\". The value should be an integer representing the recommended wait time in milliseconds.\r\n\r\n---\r\n\r\n**EXAMPLE RESPONSES**\r\n\r\n**Example for a CONTINUING turn:**\r\nI have determined the user is likely to continue because they are pausing while dictating a sequence of numbers.\r\nResult:\r\n```json\r\n{\r\n  \"Classification\": \"CONTINUE\",\r\n  \"Reasoning\": \"User is dictating a confirmation code, indicating high cognitive load.\",\r\n  \"WaitBeforeConsideringEndMilliseconds\": 2800\r\n}\r\n```\r\n\r\n**Example for an ENDED turn:**\r\nThe user has finished their turn by asking a complete and direct question.\r\nResult:\r\n```json\r\n{\r\n  \"Classification\": \"END\",\r\n  \"Reasoning\": \"User has asked a complete question.\"\r\n}\r\n```\r\n\r\n---\r\n\r\n**ANALYSIS HEURISTICS & RULES**\r\n\r\nYour analysis based on these rules should inform both your top-level explanation and the `Reasoning` field within the JSON.\r\n\r\n**1. Criteria for \"Classification: END\"**\r\n*   The user has formed a complete sentence, question, or command.\r\n*   The user's intonation suggests finality (infer this from complete thoughts).\r\n*   **Crucial Rule:** The utterance consists of phrases used to regain the AI's attention, such as \"hello?\", \"are you there?\", \"did you get that?\", or similar. This strongly indicates the user *already* finished their turn. Classify this as 'END' with high priority.\r\n\r\n**2. Criteria for \"Classification: CONTINUE\"**\r\n*   The utterance is grammatically incomplete (e.g., \"I was thinking about the...\").\r\n*   The user ends on a conjunction like \"and\", \"but\", \"so\", or \"because\".\r\n*   The user uses filler words like \"um\", \"uh\", or \"like\" followed by a pause.\r\n\r\n**3. Determining \"WaitBeforeConsideringEndMilliseconds\"**\r\nThis value is critical and must be determined based on the context of the pause.\r\n*   **Standard Pauses (700-1200ms):** For a typical mid-sentence pause to breathe or think.\r\n    *   *Example:* \"I need to book a flight to Jakarta and, um... [pause]\" -> `\"WaitBeforeConsideringEndMilliseconds\": 900`\r\n*   **Cognitive Load Pauses (1500-3000ms):** When the user is performing a mentally demanding task, provide a more generous wait time. This applies when the user is:\r\n    *   Recalling or dictating numbers (phone, ID, etc.).\r\n    *   Spelling out a name or an email address.\r\n    *   Recalling a specific memory or formulating a complex idea.\r\n    *   *Example:* \"My email address is myname... [pause] ...@gmail.com\" -> `\"WaitBeforeConsideringEndMilliseconds\": 2500`\r\n");
            _turnEndLLMService.MessageStreamed += async (sender, e) =>
            {
                await OnAITurnEndLLMMessageStream(sender, e);
            };
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

            var llmResult = await _llmProviderManager.BuildProviderServiceByIntegration(_loggerFactory, integrationDataResult.Data, llmConfig, new Dictionary<string, string>());
            if (!llmResult.Success || llmResult.Data == null)
            {
                _logger.LogError("Failed to build LLM service for AI Turn End: {Message}. Falling back to disabling verification.", llmResult.Message);
                _config.Verification.Enabled = false;
                return;
            }

            _interruptionVerificationLLMService = llmResult.Data;
            _interruptionVerificationLLMService.SetSystemPrompt("You are an expert at analyzing real-time speech transcripts. Your task is to determine if a user speech should interrupt the agent speech. Analyze the user's utterance. Respond with ONLY ONE of the following words: 'CONTINUE' if the user speech should not interrupt the agent. 'INTERRUPT' if the user has likely interrupted the agent.");
            _interruptionVerificationLLMService.MessageStreamed += async (sender, e) =>
            {
                await OnAIInterruptionVerificationMessageStream(sender, e);
            };
        }
        public async Task ResetForNewTurn()
        {
            await _transcriptionProcessingLock.WaitAsync();

            try
            {
                _logger.LogDebug("Agent {AgentId}: Resetting for new turn.", _agentState.AgentId);

                CancelMlFallbackTimer();

                if (_config.UseTurnByTurnMode && !_config.IncludeInterruptedSpeechInTurnByTurnMode!.Value)
                {
                    _userTurnTextBuffer.Clear();
                    _userTurnTextFinalBuffer.Clear();
                }

                if (_interruptionVerificationLLMTask != null)
                {
                    try
                    {
                        _interruptionVerificationLLMCTS.Cancel();
                    }
                    catch { /* Expected */ }
                    _interruptionVerificationLLMTask = null;
                }

                if (_turnEndLLMTask != null)
                {
                    try
                    {
                        _turnEndLLMCTS.Cancel();
                    }
                    catch { /* Expected */ }
                    _turnEndLLMTask = null;
                }

                _userTurnAudioBuffer.Clear();
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
                _turnEndLLMInputBuffer.Clear();
                _interruptionVerificationLLMInputBuffer.Clear();
            }
            finally
            {
                _transcriptionProcessingLock.Release();
            }
        }
        public void SetUserTurnActive()
        {
            _isUserTurnActive = true;
        }

        // Management
        public async Task ProcessTranscriptionForTurnAnalysis(string text, bool isFinal)
        {
            await _transcriptionProcessingLock.WaitAsync();

            try
            {
                if (_agentState.IsVoicemailDetected) return;
                if (_agentState.AreTurnsPaused) return;

                await CreateNewUserTurn();

                if (isFinal)
                {
                    _userTurnTextFinalBuffer.Append(text);
                }
                else
                {
                    _userTurnTextBuffer.Append(text);
                }

                var currentUtterance = isFinal ? _userTurnTextFinalBuffer.ToString() : _userTurnTextBuffer.ToString();

                // is not turn by turn
                if (!_config.UseTurnByTurnMode)
                {
                    if (_agentState.CurrentTurn.Type == ConversationTurnType.System || _agentState.CurrentTurn.Type == ConversationTurnType.ToolResult)
                    {
                        // do nothing when its system or tool result type
                        return;
                    }

                    if (_agentState.CurrentTurn.Type == ConversationTurnType.User && _agentState.CurrentTurn.Status == ConversationTurnStatus.AgentExecutingTool)
                    {
                        // do nothing when tool is being executed
                        return;
                    }

                    if (
                        (_agentState.CurrentTurn.Status == ConversationTurnStatus.AgentProcessing || _agentState.CurrentTurn.Status == ConversationTurnStatus.AgentRespondingSpeech)
                        && !_isAgentPaused
                    )
                    {
                        HandleSTTPauseTrigger(currentUtterance);
                    }
                }
                // is turn by turn mode
                else if (
                    _agentState.CurrentTurn.Status == ConversationTurnStatus.AgentProcessing ||
                    _agentState.CurrentTurn.Status == ConversationTurnStatus.AgentRespondingSpeech ||
                    _agentState.CurrentTurn.Status == ConversationTurnStatus.AgentExecutingTool
                )
                {
                    // do nothing if its agent turn
                    return;
                }

                if (isFinal)
                {
                    _logger.LogDebug("Agent {AgentId}: Final transcript recived for turn {TurnId}: {Transcript}.", _agentState.AgentId, _agentState.CurrentTurn.Id, currentUtterance);

                    if (string.IsNullOrEmpty(currentUtterance))
                    {
                        _logger.LogError("Agent {AgentId}: Final transcript is empty for turn {TurnId}.", _agentState.AgentId, _agentState.CurrentTurn.Id);
                        return;
                    }

                    _sttHasProvidedFinalTranscript = true;
                    if (_config.TurnEnd.Type == AgentInterruptionTurnEndTypeENUM.AI)
                    {
                        await CheckAITurnEndAsync(currentUtterance);
                    }
                    else
                    {
                        await TryConcludeUserTurn();
                    }
                }
            }
            finally
            {
                _transcriptionProcessingLock.Release();
            }
        }
        public async Task ProcessDirectTextInputAsync(string text, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();

            await _transcriptionProcessingLock.WaitAsync(cancellationToken);

            try
            {
                var now = DateTime.UtcNow;
                var newTurn = new ConversationTurn
                {
                    Type = ConversationTurnType.User,
                    UserInput = new ConversationTurnUserInput
                    {
                        SenderId = _agentState.CurrentClientId ?? "UnknownClient",
                        TranscribedText = text,
                        StartedSpeakingAt = now,
                        FinishedSpeakingAt = now,
                    },
                    Response = new ConversationTurnAgentResponse()
                    {
                        AgentId = _agentState.BusinessAppAgent!.Id
                    },
                    Status = ConversationTurnStatus.UserInputEnded
                };

                if (NewTurnCreated != null)
                {
                    await NewTurnCreated.Invoke(newTurn);
                }

                if (_agentState.IsResponding || _agentState.IsExecutingCustomTool || _agentState.IsExecutingSystemTool)
                {
                    newTurn.Status = ConversationTurnStatus.Interrupted;

                    if (VerifiedInterruptionOccurred != null)
                    {
                        // this is wrong, this needs to be the current turn not the new turn
                        //await VerifiedInterruptionOccurred.Invoke(newTurn, text);
                    }
                }
                else
                {
                    if (UserTurnFinalized != null)
                    {
                        await UserTurnFinalized.Invoke(newTurn);
                    }
                }
            }
            finally
            {
                _transcriptionProcessingLock.Release();
            }
        }
        public void NotifyAgentSpeechCompleted()
        {
            _isAgentPaused = false;
            _pauseTriggerVadTracker?.Reset();
        }
        public void BufferAudioForMlAnalysis(byte[] audioData)
        {
            // Only buffer if the ML service is active and a user turn has started.
            if (_mlTurnService != null && _config.TurnEnd.Type == AgentInterruptionTurnEndTypeENUM.ML)
            {
                _userTurnAudioBuffer.AddRange(audioData);
            }
        }

        // Common Helper
        private async Task<ConversationTurn?> CreateNewUserTurn()
        {
            if (!_isUserTurnActive)
            {
                _isUserTurnActive = true;

                _vadHasIndicatedTurnEnd = false;
                _sttHasProvidedFinalTranscript = false;
                _aiHasIndicatedTurnEnd = false;
                _mlHasIndicatedTurnEnd = false;

                var newTurn = new ConversationTurn
                {
                    Type = ConversationTurnType.User,
                    Status = ConversationTurnStatus.UserInputStarted,
                    UserInput = new ConversationTurnUserInput
                    {
                        SenderId = _agentState.CurrentClientId ?? "UnknownClient",
                        StartedSpeakingAt = DateTime.UtcNow
                    },
                    Response = new ConversationTurnAgentResponse()
                    {
                        AgentId = _agentState.BusinessAppAgent!.Id
                    }
                };

                if (NewTurnCreated != null)
                {
                    await NewTurnCreated.Invoke(newTurn);
                }

                return newTurn;
            }

            return null;
        }
        private async Task TryConcludeUserTurn()
        {
            if (_agentState.IsVoicemailDetected) return;
            if (_agentState.AreTurnsPaused) return;

            var turnToFinalize = _agentState.CurrentTurn;
            if (turnToFinalize == null)
            {
                _logger.LogError("TryConcludeUserTurn was called, but no active turn exists in the agent state.");
                ResetForNewTurn();
                return;
            }

            _logger.LogDebug("Agent {AgentId}: Attempting to conclude user turn {TurnId}", _agentState.AgentId, turnToFinalize.Id);

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
                _logger.LogDebug("Agent {AgentId}: Conditions not yet met for concluding user turn.", _agentState.AgentId);
                return; // Conditions not yet met.
            }     

            var finalText = _userTurnTextFinalBuffer.ToString().Trim();

            if (!_isAgentPaused)
            {
                turnToFinalize.UserInput.TranscribedText = finalText;
                turnToFinalize.UserInput.FinishedSpeakingAt = DateTime.UtcNow;
                turnToFinalize.Status = ConversationTurnStatus.UserInputEnded;

                if (UserTurnFinalized != null)
                {
                    _logger.LogDebug("Agent {AgentId}: Finalizing user turn with id {TurnId}.", _agentState.AgentId, turnToFinalize.Id);
                    _ = UserTurnFinalized.Invoke(turnToFinalize);
                }
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
                                await OnVerifiedInterruption(turnToFinalize, finalText);
                            }
                            else if (AgentShouldResume != null)
                            {
                                _userTurnTextBuffer.Clear();
                                _userTurnTextFinalBuffer.Clear();
                                _sttHasProvidedFinalTranscript = false;

                                await AgentShouldResume.Invoke();
                                _isAgentPaused = false;
                            }

                            _isAwaitingVerification = false;
                            _hasVerifiedInterruptionResult = false;
                            _canInterruptAgentAfterVerificaiton = false;
                        }

                        return;
                    }
                    else
                    {
                        _isAwaitingVerification = true;
                        _hasVerifiedInterruptionResult = false;
                        _canInterruptAgentAfterVerificaiton = false;
                        _ = CheckAIInterruptionVerificationAsync(finalText);
                        return;
                    }
                }
                else
                {
                    await OnVerifiedInterruption(turnToFinalize, finalText);
                }
            }

            _userTurnTextBuffer.Clear();
            _userTurnTextFinalBuffer.Clear();
            _sttHasProvidedFinalTranscript = false;
        }
        private string CalculatePreviousTurnAgentSpokenText()
        {
            if (_agentState.PreviousTurn == null)
            {
                _logger.LogError("CalculatePreviousTurnAgentSpokenText was called, but no active turn exists in the agent state.");
                return "";
            }

            string currentSpeechSegments = "";
            foreach (var speechSegment in _agentState.PreviousTurn.Response.SpokenSegments)
            {
                currentSpeechSegments += speechSegment.Text;
            }

            return currentSpeechSegments;
        }
        private async Task OnVerifiedInterruption(ConversationTurn turnToInterrupt, string interruptingText)
        {
            await VerifiedInterruptionOccurred!?.Invoke(turnToInterrupt);

            var newTurn = await CreateNewUserTurn();
            newTurn!.UserInput.TranscribedText = interruptingText;

            await UserTurnFinalized!.Invoke(newTurn);
        }

        // Pause Trigger Event Handlers
        private void HandleSTTPauseTrigger(string newTextChunk)
        {
            if (_agentState.IsVoicemailDetected) return;
            if (_agentState.AreTurnsPaused) return;

            if (_config.PauseTrigger?.Type == AgentInterruptionPauseTriggerTypeENUM.STT)
            {
                var wordCountThreshold = _config.PauseTrigger.WordCount ?? 0;
                if (wordCountThreshold > 0)
                {
                    _currentTurnWordCount += newTextChunk.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;

                    if (_currentTurnWordCount >= wordCountThreshold)
                    {
                        AgentShouldPause?.Invoke();
                        _isAgentPaused = true;
                    }
                }
            }
        }
        private void OnPauseTriggerVadSpeechStarted(TimeSpan duration)
        {
            if (_agentState.IsVoicemailDetected) return;
            if (_agentState.AreTurnsPaused) return;

            if (_agentState.CurrentTurn == null) return;

            if (
                _agentState.CurrentTurn.Type == ConversationTurnType.User &&
                (_agentState.CurrentTurn.Status == ConversationTurnStatus.AgentProcessing || _agentState.CurrentTurn.Status == ConversationTurnStatus.AgentRespondingSpeech)
                && !_isAgentPaused
                )
            {
                AgentShouldPause?.Invoke();
                _isAgentPaused = true;
            }
        }

        // Vad Turn End Event Handlers
        private async Task OnTurnEndVadSpeechStarted()
        {
            await _transcriptionProcessingLock.WaitAsync();

            try
            {
                if (_agentState.IsVoicemailDetected) return;
                if (_agentState.AreTurnsPaused) return;

                await CreateNewUserTurn();
            }
            finally
            {
                _transcriptionProcessingLock.Release();
            }
        }
        private async Task OnTurnEndVadSpeechEnded()
        {
            await _transcriptionProcessingLock.WaitAsync();

            try
            {
                if (_agentState.IsVoicemailDetected) return;
                if (_agentState.AreTurnsPaused) return;

                if (_config.TurnEnd.Type == AgentInterruptionTurnEndTypeENUM.VAD && _isUserTurnActive)
                {
                    _vadHasIndicatedTurnEnd = true;
                    await TryConcludeUserTurn();
                }
            }
            finally
            {
                _transcriptionProcessingLock.Release();
            }
        }
        // AI Turn End
        private async Task CheckAITurnEndAsync(string currentUtterance)
        {
            if (_turnEndLLMService == null || string.IsNullOrWhiteSpace(currentUtterance))
            {
                _logger.LogWarning("Agent {AgentId}: AI Turn End check skipped, LLM service is null or current utterance is empty.", _agentState.AgentId);
                return;
            }

            _logger.LogDebug("Agent {AgentId}: Checking AI Turn End.", _agentState.AgentId);

            // Cancel any previous, now-outdated check
            try
            {
                _turnEndLLMCTS.Cancel();
                if (_turnEndLLMTask != null)
                {
                    _logger.LogDebug("Agent {AgentId}: AI Turn End check already running, canceling.", _agentState.AgentId);
                    await _turnEndLLMTask;
                }
            }
            catch { }
            _turnEndLLMCTS = CancellationTokenSource.CreateLinkedTokenSource(_agentState.MasterCancellationToken);
            _turnEndLLMTask = null;
            if (_turnEndLLMInputBuffer.Length > 0) _turnEndLLMInputBuffer.Clear();

            try
            {
                _turnEndLLMService.ClearMessages();

                var requestText = $"**TASK**\n\nAnalyze the user utterance provided below and respond ONLY with the classification and, if applicable, the wait time, following the exact format specified.\n\nAgent Utterance: \"{CalculatePreviousTurnAgentSpokenText()}\"\nUser Utterance: \"{currentUtterance}\"";
                
                _turnEndLLMService.AddUserMessage(requestText);
                _turnEndLLMTask = _turnEndLLMService.ProcessInputAsync(_turnEndLLMCTS.Token);
                _logger.LogDebug("Agent {AgentId}: AI Turn End check llm process started.", _agentState.AgentId);
                await _turnEndLLMTask;
            }
            catch (OperationCanceledException){ /* Expected */ }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during AI turn end check.");
            }
        }
        private async Task OnAITurnEndLLMMessageStream(object? sender, ConversationAgentEventLLMStreamed? data)
        {
            try
            {
                if (_turnEndLLMCTS.IsCancellationRequested) return;

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
                    var fullResponseText = _turnEndLLMInputBuffer.ToString();
                    TurnEndResponse? turnEndData = null;

                    try
                    {
                        string jsonText = string.Empty;

                        const string jsonMarker = "```json";
                        const string genericMarker = "```";

                        int startIndex = fullResponseText.IndexOf(jsonMarker, StringComparison.OrdinalIgnoreCase);
                        int endIndex = -1;

                        if (startIndex != -1)
                        {
                            startIndex += jsonMarker.Length; // Move past the marker itself.
                            endIndex = fullResponseText.LastIndexOf(genericMarker);
                        }
                        else
                        {
                            //  If "```json" isn't found, try to find a generic "```" block.
                            startIndex = fullResponseText.IndexOf(genericMarker);
                            if (startIndex != -1)
                            {
                                startIndex += genericMarker.Length;
                                endIndex = fullResponseText.LastIndexOf(genericMarker);
                            }
                        }

                        if (startIndex != -1 && endIndex > startIndex)
                        {
                            jsonText = fullResponseText.Substring(startIndex, endIndex - startIndex).Trim();
                        }
                        else
                        {
                            int firstBrace = fullResponseText.IndexOf('{');
                            int lastBrace = fullResponseText.LastIndexOf('}');
                            if (firstBrace != -1 && lastBrace > firstBrace)
                            {
                                jsonText = fullResponseText.Substring(firstBrace, lastBrace - firstBrace + 1).Trim();
                            }
                        }

                        if (string.IsNullOrWhiteSpace(jsonText))
                        {
                            throw new JsonException("Could not find a valid JSON code block or object in the LLM response.");
                        }

                        turnEndData = JsonSerializer.Deserialize<TurnEndResponse>(jsonText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Agent {AgentId}: Failed to parse AI Turn End response. Defaulting to END. Raw response: {RawResponse}", _agentState.AgentId, fullResponseText);
                        _aiHasIndicatedTurnEnd = true;
                        _turnEndLLMTask = null;
                        await TryConcludeUserTurn();
                        return;
                    }

                    switch (turnEndData.Classification.ToUpperInvariant())
                    {
                        case "END":
                            _logger.LogDebug("Agent {AgentId}: AI Turn End detected. Reason: {Reason}", _agentState.AgentId, turnEndData.Reasoning);
                            _aiHasIndicatedTurnEnd = true;
                            _turnEndLLMTask = null;
                            await TryConcludeUserTurn();
                            break;

                        case "CONTINUE":
                            _logger.LogDebug("Agent {AgentId}: AI detected user turn is continuing. Reason: {Reason}. Suggested wait: {WaitMs}ms",
                                _agentState.AgentId, turnEndData.Reasoning, turnEndData.WaitBeforeConsideringEndMilliseconds ?? 0);
                            _aiHasIndicatedTurnEnd = false;
                            _turnEndLLMTask = null;
                            break;

                        default:
                            _logger.LogWarning("Agent {AgentId}: AI returned unknown classification '{Classification}'. Defaulting to END.", _agentState.AgentId, turnEndData.Classification);
                            _aiHasIndicatedTurnEnd = true;
                            _turnEndLLMTask = null;
                            await TryConcludeUserTurn();
                            break;
                    }
                }
            }
            catch (OperationCanceledException){ /* Expected */ }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during AI turn end check.");
            }
        }
        // AI Interuption Verification
        private async Task CheckAIInterruptionVerificationAsync(string currentUtterance)
        {
            if (_interruptionVerificationLLMService == null || string.IsNullOrWhiteSpace(currentUtterance))
            {
                _logger.LogWarning("Agent {AgentId}: AI Interruption Verification check skipped, LLM service is null or current utterance is empty.", _agentState.AgentId);
                return;
            }

            _logger.LogDebug("Agent {AgentId}: Checking AI Interruption Verification.", _agentState.AgentId);

            // Cancel any previous, now-outdated check
            try
            {
                _interruptionVerificationLLMCTS.Cancel();
                if (_interruptionVerificationLLMTask != null)
                {
                    _logger.LogDebug("Agent {AgentId}: AI Interruption Verification check already running, canceling.", _agentState.AgentId);
                    await _interruptionVerificationLLMTask;
                }
            }
            catch { }
            _interruptionVerificationLLMCTS = CancellationTokenSource.CreateLinkedTokenSource(_agentState.MasterCancellationToken);
            if (_interruptionVerificationLLMInputBuffer.Length > 0) _interruptionVerificationLLMInputBuffer.Clear();

            try
            {
                _interruptionVerificationLLMService.ClearMessages();

                // TODO based on the current turn, calculate how much of the current text was spoken and pass that
                var requestText = $"Agent Previous Utterance: \"{CalculatePreviousTurnAgentSpokenText()}\"\n\nUser Utterance: \"{currentUtterance}\"";

                _interruptionVerificationLLMService.AddUserMessage(requestText);
                _interruptionVerificationLLMTask = _interruptionVerificationLLMService.ProcessInputAsync(_interruptionVerificationLLMCTS.Token);
                _logger.LogDebug("Agent {AgentId}: AI Interruption Verification check started.", _agentState.AgentId);
                await _interruptionVerificationLLMTask;
            }
            catch (OperationCanceledException) { /* Expected */ }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during AI interruption verification check.");
            }
        }
        private async Task OnAIInterruptionVerificationMessageStream(object? sender, ConversationAgentEventLLMStreamed? data)
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
                    _logger.LogDebug("Agent {AgentId}: AI interruption verification result: INTERRUPT.", _agentState.AgentId);
                    _canInterruptAgentAfterVerificaiton = true;
                }
                else
                {
                    _logger.LogDebug("Agent {AgentId}: AI interruption verification result: NO INTERRUPT.", _agentState.AgentId);
                    _canInterruptAgentAfterVerificaiton = false;
                }
                await TryConcludeUserTurn();
            }
        }
        // ML Turn End
        private async Task OnMLTurnVadSpeechStarted()
        {
            await _transcriptionProcessingLock.WaitAsync();

            try
            {
                if (_agentState.IsVoicemailDetected) return;
                if (_agentState.AreTurnsPaused) return;

                await CreateNewUserTurn();
            }
            finally
            {
                _transcriptionProcessingLock.Release();
            }
        }
        private async Task OnMLTurnVadSpeechEnded()
        {
            await _transcriptionProcessingLock.WaitAsync();

            try
            {
                if (_agentState.IsVoicemailDetected) return;
                if (_agentState.AreTurnsPaused) return;

                if (_isUserTurnActive && _mlTurnService != null && _userTurnAudioBuffer.Count > 0)
                {
                    var turnAudio = _userTurnAudioBuffer.ToArray();
                    StartMlFallbackTimer();
                    _mlTurnService.AnalyzeTurn(turnAudio);
                }
            }
            finally
            {
                _transcriptionProcessingLock.Release();
            }
        }
        private async Task OnMlTurnEnded()
        {
            CancelMlFallbackTimer();

            if (_isUserTurnActive && !_mlHasIndicatedTurnEnd)
            {
                _mlHasIndicatedTurnEnd = true;
                await TryConcludeUserTurn();
            }
        }
        private void StartMlFallbackTimer()
        {
            // Ensure any previous timer is stopped before starting a new one.
            CancelMlFallbackTimer();

            var fallbackMs = _config.TurnEnd.MlTurnEndFallbackMs.Value;
            _mlTurnFallbackCts = new CancellationTokenSource();
            var token = _mlTurnFallbackCts.Token;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(fallbackMs, token);

                    if (_mlTurnFallbackCts == null || _mlTurnFallbackCts.IsCancellationRequested) return;

                    await OnMlTurnEnded();
                }
                catch (OperationCanceledException) { /* Expected */ }
            }, token);
        }
        private void CancelMlFallbackTimer()
        {
            if (_mlTurnFallbackCts != null && !_mlTurnFallbackCts.IsCancellationRequested)
            {
                _mlTurnFallbackCts.Cancel();
                _mlTurnFallbackCts.Dispose();
                _mlTurnFallbackCts = null;
            }
        }

        // Disposal
        public void Dispose()
        {
            CancelMlFallbackTimer();
            if (_agentState.SileroVadCore != null)
            {
                if (_turnEndVadTracker != null) _agentState.SileroVadCore.SpeechProbabilityUpdated -= _turnEndVadTracker.ProcessProbability;
                if (_pauseTriggerVadTracker != null) _agentState.SileroVadCore.SpeechProbabilityUpdated -= _pauseTriggerVadTracker.ProcessProbability;
            }
            _turnEndLLMCTS?.Cancel();
            _interruptionVerificationLLMCTS?.Cancel();
            try
            {
                (_turnEndLLMService as IDisposable)?.Dispose();
            }
            catch (ObjectDisposedException) { /* Expected */ }

            try
            {
                (_interruptionVerificationLLMService as IDisposable)?.Dispose();
            }
            catch (ObjectDisposedException) { /* Expected */ }
        }
    }

    internal class TurnEndResponse
    {
        [JsonPropertyName("Classification")]
        public string Classification { get; set; }

        [JsonPropertyName("Reasoning")]
        public string Reasoning { get; set; }

        [JsonPropertyName("WaitBeforeConsideringEndMilliseconds")]
        public int? WaitBeforeConsideringEndMilliseconds { get; set; }
    }
}