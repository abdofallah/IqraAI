using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.VAD;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.LLM.Providers.Helpers;
using Microsoft.Extensions.Logging;
using System.Text;
using IqraInfrastructure.Managers.VAD;
using IqraInfrastructure.Managers.Business;
using IqraCore.Entities.Helper.Agent;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Business;
using System.Diagnostics;


namespace IqraInfrastructure.Managers.Conversation.Session.Agent.AI
{
    public class ConversationAIAgentInterruptionManager : IDisposable
    {
        // Dependencies
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ConversationAIAgentInterruptionManager> _logger;
        private readonly ConversationAIAgentState _agentState;
        private readonly LLMProviderManager _llmProviderManager;
        private readonly BusinessManager _businessManager;

        // References to other modules for coordination
        private readonly ConversationAIAgentAudioOutput _audioOutput;
        private readonly ConversationAIAgentLLMHandler _llmHandler;

        // VAD State & Timer
        private CancellationTokenSource? _vadInterruptTimerCTS = null;
        private Task? _vadInterruptTimerTask = null;

        // Interrupting LLM State
        public BusinessAppIntegration? _interruptionLLMIntegration { get; internal set; }
        private Task? _interruptLLMTask = null;
        private StringBuilder _interruptViAILLMBuilder = new StringBuilder();

        private readonly SemaphoreSlim _vadActivityStatusLock = new SemaphoreSlim(1, 1);

        // VAD service managed here
        private IVadService? _vadService;
        private VadOptions? _vadOptions;

        public ConversationAIAgentInterruptionManager(
            ILoggerFactory loggerFactory,
            ConversationAIAgentState agentState,
            LLMProviderManager llmProviderManager,
            BusinessManager businessManager,
            ConversationAIAgentAudioOutput audioOutput,
            ConversationAIAgentLLMHandler llmHandler
        )
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<ConversationAIAgentInterruptionManager>();
            _agentState = agentState;
            _llmProviderManager = llmProviderManager;
            _businessManager = businessManager;
            _audioOutput = audioOutput;
            _llmHandler = llmHandler;
        }

        public async Task InitializeAsync(CancellationToken agentCTS)
        {
            try
            {
                if (_agentState.CurrentConversationType == AgentInterruptionTypeENUM.TurnByTurn)
                {
                    return;
                }

                if (
                    _agentState.CurrentConversationType == AgentInterruptionTypeENUM.InterruptibleViaVAD
                )
                {
                    _logger.LogInformation("Agent {AgentId}: Initializing VAD for interruptible conversation.", _agentState.AgentId);
                    _vadOptions = new VadOptions { SampleRate = 16000 }; // TODO: Configurable? SampleRate from AudioOutput const?
                    string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VadModels\\silero_vad.onnx"); // TODO: Make model path configurable
                    if (!File.Exists(modelPath))
                    {
                        _logger.LogError("Agent {AgentId}: VAD model file not found at {Path}", _agentState.AgentId, modelPath);
                        // Decide how to handle - throw, or disable interruption? Disable for now.
                        _agentState.CurrentConversationType = AgentInterruptionTypeENUM.InterruptibleViaResponse; // Fallback
                    }
                    else
                    {
                        DisposeCurrentVadService();

                        _vadService = new SileroVadService(_loggerFactory.CreateLogger<SileroVadService>());
                        _vadService.Initialize(modelPath, _vadOptions);
                        _vadService.VoiceActivityChanged += OnVoiceActivityChanged;

                        _agentState.VadService = _vadService;
                        _agentState.IsVadEnabled = true;

                        _logger.LogInformation("Agent {AgentId}: VAD Initialized.", _agentState.AgentId);
                    }
                }

                if (_agentState.CurrentConversationType == AgentInterruptionTypeENUM.InterruptibleViaAI)
                {
                    await InitializeInterruptingLLMAsync();
                }
            }
            finally
            {
                _logger.LogInformation("Interruption Manager initialized for Agent {AgentId}.", _agentState.AgentId);
            }         
        }

        private async Task InitializeInterruptingLLMAsync()
        {
            _logger.LogDebug("Agent {AgentId}: Setting up interrupting LLM.", _agentState.AgentId);

            BusinessAppRouteAgentInterruptionViaAI interruptionData = (BusinessAppRouteAgentInterruptionViaAI)_agentState.CurrentSessionContext?.Agent.Interruption;

            FunctionReturnResult<ILLMService?> interuptibleLLMServiceResult;
            if (interruptionData.UseCurrentAgentLLMForInterrupting)
            {
                // Ensure primary LLM and its integration data are available
                if (_agentState.LLMService == null || _agentState.LLMBusinessIntegrationData == null || _agentState.BusinessAppAgent == null)
                {
                    _logger.LogError("Agent {AgentId}: Cannot reuse primary LLM for interruption - primary LLM not initialized.", _agentState.AgentId);
                    throw new InvalidOperationException("Primary LLM required for reuse in interruption.");
                }
                var defaultLLMServiceInfo = _agentState.BusinessAppAgent.Integrations.LLM[_agentState.CurrentLanguageCode][0];
                interuptibleLLMServiceResult = await _llmProviderManager.BuildProviderServiceByIntegration(
                   _agentState.LLMBusinessIntegrationData,
                   defaultLLMServiceInfo,
                   new Dictionary<string, string> { }
                 );
            }
            else
            {
                var interruptionLLMIntegrationResult = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(_agentState.BusinessApp.Id, interruptionData.LLMIntegrationToUseForCheckingInterruption.Id);
                if (!interruptionLLMIntegrationResult.Success || interruptionLLMIntegrationResult.Data == null)
                {
                    _logger.LogError("Agent {AgentId}: Interruption LLM integration {IntegrationId} not found", _agentState.AgentId, interruptionData.LLMIntegrationToUseForCheckingInterruption.Id);
                    throw new InvalidOperationException($"Interruption LLM integration {interruptionData.LLMIntegrationToUseForCheckingInterruption.Id} not found");
                }
                _interruptionLLMIntegration = interruptionLLMIntegrationResult.Data;

                interuptibleLLMServiceResult = await _llmProviderManager.BuildProviderServiceByIntegration(_interruptionLLMIntegration, interruptionData.LLMIntegrationToUseForCheckingInterruption, new Dictionary<string, string> { });  
            }

            if (!interuptibleLLMServiceResult.Success || interuptibleLLMServiceResult.Data == null)
            {
                _logger.LogError("Agent {AgentId}: Failed to build Interruption LLM service with error: {ErrorMessage}", _agentState.AgentId, interuptibleLLMServiceResult.Message);
                throw new InvalidOperationException($"Failed to build Interruption LLM service: {interuptibleLLMServiceResult.Message}");
            }

            DisposeCurrentInterruptionLLMService(_agentState.InterruptingLLMService);
            _agentState.InterruptingLLMService = interuptibleLLMServiceResult.Data;

            // todo - move the prompt to the backend prompt management system
            _agentState.InterruptingLLMService.SetSystemPrompt("You are a neural pathway inside a brain of a customer support agent. Your task is to decide that when the customer on call interrupts you whether you should keep speaking or allow the user to interrupt.\n\nYou must always think first and provide your think to why you think the customer response should interrupt you mid response.\n\nHere are the guidelines for thinking before a response (for the thoughts of the model)\n<ThinkingGuidelines>\n\t- The thinking text should always be summarized and not exceed more than 1 to 2 sentences.\n\t- The thinking text should always be less than maximum of 100 characters.\n</ThinkingGuidelines>\n\nYou must follow the following format to reply back:\n```\nthinking: <your rapid thoughts>\nresult: <allow_interrupt or continue_speaking>\n```\nHere are the results format:\nIf we should let the customer speak, respond back with: \"allow_interrupt\".\nIf we should let the customer support agent speak, respond back with: \"continue_speaking\".");
            _logger.LogInformation("Agent {AgentId}: Interrupting LLM Initialized.", _agentState.AgentId);
        }
        public async Task ReInitializeForLanguageAsync()
        {
            _logger.LogInformation("Agent {AgentId}: Re-initializing Interruption Manager for new language.", _agentState.AgentId);

            if (_agentState.CurrentConversationType == AgentInterruptionTypeENUM.InterruptibleViaAI)
            {
                await InitializeInterruptingLLMAsync();
            }
        }

        // Called by Orchestrator's ProcessTextAsync when text arrives WHILE agent is speaking
        public async Task<bool> CheckShouldLetAgentBeInterrupted(string text, string? clientId, CancellationToken externalToken)
        {
            if (_agentState.CurrentConversationType == AgentInterruptionTypeENUM.TurnByTurn)
            {
                if (
                    _agentState.IsResponding
                    ||
                    _agentState.IsExecutingCustomTool || _agentState.IsRespondingCustomToolRespone
                    ||
                    _agentState.IsExecutingSystemTool || _agentState.IsRespondingSystemToolRespone
                )
                {
                    if (((BusinessAppRouteAgentInterruptionTurnByTurn)_agentState.CurrentSessionContext.Agent.Interruption).UseInterruptedResponseInNextTurn)
                    {
                        _agentState.InterruptResponseBuffer.Append($"(Spoken during your turn [{text}])");
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }

            if (_agentState.CurrentConversationType == AgentInterruptionTypeENUM.InterruptibleViaResponse)
            {
                await _llmHandler.CancelCurrentLLMTaskAsync();

                // todo can make 100 configurable by user maybe?
                var audioLeftToPlay = _audioOutput.CurrentlyLeftToPlay();
                if (audioLeftToPlay > TimeSpan.Zero)
                {
                    var amountToWait = Math.Min(200, (int)_agentState.CurrentResponseDurationSpeakingStarted.Value.Add(audioLeftToPlay).Subtract(DateTime.UtcNow).TotalMilliseconds);
                    await _audioOutput.StartVolumeFadeAsync(0.0f, TimeSpan.FromMilliseconds(amountToWait), externalToken);
                    await Task.Delay(amountToWait);
                    await _audioOutput.CancelCurrentSpeechPlaybackAsync();
                }       

                await _audioOutput.StartVolumeFadeAsync(1.0f, TimeSpan.FromMilliseconds(1), externalToken);

                return false;
            }

            if (_agentState.CurrentConversationType == AgentInterruptionTypeENUM.InterruptibleViaVAD)
            {
                // Agent is responding and user starts speaking, we will go ahead and buffer the text
                // we will let OnVoiceActivityChanged decide what to do

                if (!_agentState.IsResponding)
                {
                    await _llmHandler.CancelCurrentLLMTaskAsync();
                    return false;
                }

                _logger.LogDebug("Agent {AgentId}: Buffering text during VAD interrupt check: '{Text}'", _agentState.AgentId, text);
                _agentState.InterruptResponseBuffer.Append(text + " ");
                return true;
            }

            if (_agentState.CurrentConversationType == AgentInterruptionTypeENUM.InterruptibleViaAI)
            {
                if (!_agentState.IsResponding) return false;

                if (_interruptLLMTask != null)
                {
                    _logger.LogWarning("Agent {AgentId}: Ignoring text for AI interrupt - already processing.", _agentState.AgentId);
                    _agentState.InterruptResponseBuffer.Append(text + " ");
                    // todo check this how the response really looks like
                    return true;
                }

                // --- Start AI Interruption Check ---
                _logger.LogInformation("Agent {AgentId}: Starting AI interruption check for text: '{Text}'", _agentState.AgentId, text);
                _agentState.InterruptResponseBuffer.Clear();

                string fullText = _llmHandler.GetCurrentResponseText().Substring("response_to_customer:".Length);
                string spokenSoFar = fullText;
                string unspokenSoFar = "UNKNOWN";
                if (_agentState.CurrentResponseDurationSpeakingStarted.HasValue && _agentState.CurrentResponseDuration > TimeSpan.Zero)
                {
                    TimeSpan elapsedTime = DateTime.UtcNow - _agentState.CurrentResponseDurationSpeakingStarted.Value;
                    double proportionSpoken = Math.Clamp(elapsedTime.TotalSeconds / _agentState.CurrentResponseDuration.TotalSeconds, 0.0, 1.0);
                    int spokenLength = (int)(fullText.Length * proportionSpoken);
                    spokenSoFar = spokenSoFar.Substring(0, Math.Min(spokenLength, fullText.Length));
                    int unspokenLength = spokenLength - fullText.Length;
                    if (unspokenLength > 0)
                    {
                        unspokenSoFar = text.Substring(0, unspokenLength);
                    }
                    
                    _logger.LogInformation("Agent {AgentId}: Estimated spoken so far ({Proportion:P1}): {Text}", _agentState.AgentId, proportionSpoken, spokenSoFar);
                }
                else
                {
                    _logger.LogWarning("Agent {AgentId}: Could not estimate spoken part for AI interrupt check.", _agentState.AgentId);
                }


                _agentState.InterruptingLLMService.ClearMessages();
                string builtMessage = "customer_query: {text}";

                var spokenSoFarCut = spokenSoFar.Length > 20 ? spokenSoFar.Substring(spokenSoFar.Length - 20) : spokenSoFar;
                string context = $"response_from_system: You were interrupted by the user during speaking. You spoke as far as: ...{spokenSoFarCut}...";
                _agentState.InterruptingLLMService.AddUserMessage(builtMessage);
                // TODO should we not save this message or check if already saved

                _agentState.InterruptingLLMService.MessageStreamed += async (sender, responseObj) => {
                    await CheckIfInterruptibleViaAIMessageStreamHandlerAsync(sender, responseObj, spokenSoFar, text, clientId, externalToken);
                };

                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(_agentState.MasterCancellationToken, externalToken); // Need agent master CTS access
                _interruptLLMTask = _agentState.InterruptingLLMService.ProcessInputAsync(combinedCts.Token, context);

                return true;
            }

            // Should not reach here
            return true; 
        }

        // Via VAD Interruption
        private async void OnVoiceActivityChanged(object? sender, VadEventArgs e)
        {
            try
            {
                await _vadActivityStatusLock.WaitAsync();

                _logger.LogDebug("Agent {AgentId}: VAD activity changed: SpeechDetected={IsSpeechDetected}", _agentState.AgentId, e.IsSpeechDetected);
                _agentState.IsUserSpeakingVAD = e.IsSpeechDetected;

                // agent is speaking
                if (_agentState.IsResponding)
                {
                    // user starts speaking
                    if (e.IsSpeechDetected)
                    {
                        // vad timer not running
                        if (_vadInterruptTimerTask == null)
                        {
                            int interruptMs = ((BusinessAppRouteAgentInterruptionViaVAD)_agentState.CurrentSessionContext?.Agent.Interruption).InterruptibleConversationAudioActivityDurationMS;

                            _logger.LogDebug("Agent {AgentId}: VAD Speech Start (VAD Interrupt) - Fading out agent over {ms}ms and starting timer.", _agentState.AgentId, interruptMs);

                            await _audioOutput.StartVolumeFadeAsync(0, TimeSpan.FromMilliseconds(interruptMs), _vadInterruptTimerCTS?.Token ?? CancellationToken.None);
                            StartVADInterruptTimer(TimeSpan.FromMilliseconds(interruptMs));
                        }
                        // vad timer running
                        else
                        {
                            // this should not happen
                            _logger.LogDebug("Agent {AgentId}: Agent is responding/user is speaking/timer already started = not doing anything", _agentState.AgentId);
                            return;
                        }
                    }
                    // user stops speaking
                    else
                    {
                        // vad timer not running
                        if (_vadInterruptTimerTask == null)
                        {
                            await _audioOutput.StartVolumeFadeAsync(1.0f, TimeSpan.FromMilliseconds(100), _vadInterruptTimerCTS?.Token ?? CancellationToken.None);
                            _logger.LogDebug("Agent {AgentId}: Agent is responding/user is not speaking/timer not started = just fade audio back in", _agentState.AgentId);
                            return;
                        }
                        // vad timer running
                        else
                        {
                            await _audioOutput.StartVolumeFadeAsync(1.0f, TimeSpan.FromMilliseconds(100), _vadInterruptTimerCTS?.Token ?? CancellationToken.None);
                            _logger.LogDebug("Agent {AgentId}: Agent is responding/user is not speaking/timer is running = just fade audio back in", _agentState.AgentId);
                            return;
                        }
                    }
                }
                // agent is not speaking
                else
                {
                    await _audioOutput.StartVolumeFadeAsync(1f, TimeSpan.FromMilliseconds(10), _vadInterruptTimerCTS?.Token ?? CancellationToken.None);
                    _logger.LogDebug("Agent {AgentId}: Agent is not responding = rapid fade back in", _agentState.AgentId);
                }
            }
            catch (OperationCanceledException ex)
            {
                // expected
            }
            finally
            {
                _vadActivityStatusLock.Release();
            }
        }
        private void StartVADInterruptTimer(TimeSpan interruptDelay)
        {
            _vadInterruptTimerCTS = new CancellationTokenSource();
            var token = _vadInterruptTimerCTS.Token;

            _logger.LogDebug("Agent {AgentId}: Starting VAD interrupt timer for {Delay}.", _agentState.AgentId, interruptDelay);

            _vadInterruptTimerTask = Task.Run(async () =>
            {
                try
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    while (true)
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

                        if (stopwatch.ElapsedMilliseconds >= interruptDelay.TotalMilliseconds)
                        {
                            stopwatch.Stop();
                            break;
                        }

                        await Task.Delay(10, token);
                    }

                    if (!token.IsCancellationRequested)
                    {
                        await ProcessVadInterruptAsync(token);
                    } 
                }
                catch (OperationCanceledException)
                {
                    // expected
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Agent {AgentId}: Error in VAD interrupt timer task.", _agentState.AgentId);
                }
                finally
                {
                    _vadInterruptTimerCTS?.Dispose(); // Dispose CTS when task finishes
                    _vadInterruptTimerCTS = null;
                    _vadInterruptTimerTask = null;
                    _logger.LogDebug("Agent {AgentId}: VAD interrupt timer task finished.", _agentState.AgentId);
                }
            }, token);
        }
        private async Task ProcessVadInterruptAsync(CancellationToken token)
        {
            bool shouldCancelCurrentLLMAndSpeaking = false;
            bool shouldProcessInterruptionBuffer = false;

            // User is speaking
            if (_agentState.IsUserSpeakingVAD)
            {
                // agent is responding
                if (_agentState.IsResponding)
                {
                    shouldCancelCurrentLLMAndSpeaking = true;
                    _logger.LogDebug("Agent {AgentId}: vad result = user is speaking/agent is responding = cancelling current LLM task.", _agentState.AgentId);
                }
                else // agent is not responding
                {
                    // do nothing wait for stt result
                    _logger.LogDebug("Agent {AgentId}: vad result = user is speaking/agent is not responding = doing nothing.", _agentState.AgentId);
                    return;
                }
            }
            // User no longer speaking
            else
            {
                // agent responding
                if (_agentState.IsResponding)
                {
                    // user decides to stop mid way so do nothing
                    _logger.LogDebug("Agent {AgentId}: vad result = user is not speaking/agent is responding = doing nothing.", _agentState.AgentId);
                    return;
                }
                else // agent is not responding
                {
                    // if stt is processing more data
                    if (_agentState.IsSTTRecognizing)
                    {
                        _logger.LogDebug("Agent {AgentId}: vad result = user is not speaking/agent is not responding/stt is processing = doing nothing.", _agentState.AgentId);
                        return;
                    }
                    // stt does not seem to be processing any data
                    else
                    {
                        // some vad ignored interruption data exists
                        if (_agentState.InterruptResponseBuffer.Length > 0)
                        {
                            _logger.LogDebug("Agent {AgentId}: vad result = user is not speaking/agent is not responding/stt is not processing/interruption buffer exists = processing interruption buffer.", _agentState.AgentId);
                            shouldProcessInterruptionBuffer = true;
                        }
                        else
                        {
                            _logger.LogDebug("Agent {AgentId}: vad result = user is not speaking/agent is not responding/stt is not processing/interruption buffer does not exist = doing nothing.", _agentState.AgentId);
                            return;
                        }
                    }
                }
            }

            if (shouldCancelCurrentLLMAndSpeaking)
            {
                await _llmHandler.CancelCurrentLLMTaskAsync();            

                var currentlyLeftToPlay = _audioOutput.CurrentlyLeftToPlay();
                if (currentlyLeftToPlay > TimeSpan.Zero)
                {
                    // todo can make 100 configurable by user maybe?
                    var amountToWait = 100;
                    await _audioOutput.StartVolumeFadeAsync(0.0f, TimeSpan.FromMilliseconds(amountToWait), token);
                    await Task.Delay(amountToWait);
                    await _audioOutput.CancelCurrentSpeechPlaybackAsync();
                }

                if (!_agentState.IsSTTRecognizing && _agentState.InterruptResponseBuffer.Length > 0)
                {
                    shouldProcessInterruptionBuffer = true;
                }
            }

            if (shouldProcessInterruptionBuffer)
            {
                await _audioOutput.StartVolumeFadeAsync(1.0f, TimeSpan.FromMilliseconds(50), token);

                string text = _agentState.InterruptResponseBuffer.ToString();
                _agentState.InterruptResponseBuffer.Clear();
                await _llmHandler.ProcessUserTextAsync(text, _agentState.CurrentClientId, _agentState.MasterCancellationToken);
            }
        }
        private void CancelVADInterruptTimer()
        {
            if (_vadInterruptTimerCTS != null && !_vadInterruptTimerCTS.IsCancellationRequested)
            {
                _logger.LogTrace("Agent {AgentId}: Cancelling VAD interrupt timer.", _agentState.AgentId);
                _vadInterruptTimerCTS.Cancel();

                if (_vadInterruptTimerTask != null)
                {
                    _vadInterruptTimerTask?.Wait(500);
                    _vadInterruptTimerTask = null;
                }
            }
        }

        // Via AI Interruption
        private async Task CheckIfInterruptibleViaAIMessageStreamHandlerAsync(
            object? sender,
            object responseObj,
            string spokenSoFar,
            string text,
            string? clientId,
            CancellationToken cancellationToken
        )
        {
            if (_agentState.InterruptingLLMService == null) return; // Guard
            if (!_agentState.IsResponding) return;

            try
            {
                FunctionReturnResult<(string? deltaText, bool isEndOfResponse)?> chunkExtractResult = LLMStreamingChunkDataExtractHelper.GetChunkData(responseObj, _agentState.InterruptingLLMService.GetProviderType());
                if (!chunkExtractResult.Success || !chunkExtractResult.Data.HasValue)
                {
                    _logger.LogError("Agent {AgentId}: Error extracting interrupting LLM chunk: {Reason}", _agentState.AgentId, chunkExtractResult.Message);
                    return;
                }
                (string? deltaText, bool isEndOfResponse) = chunkExtractResult.Data.Value;

                if (!string.IsNullOrEmpty(deltaText))
                {
                    _interruptViAILLMBuilder.Append(deltaText);
                }

                string result = _interruptViAILLMBuilder.ToString().ToLower().Trim();

                if (result.Contains("allow_interrupt"))
                {
                    _agentState.InterruptingLLMService.ClearMessageStreamed();          

                    _logger.LogInformation("Agent {AgentId}: AI decided to allow interrupt. {text}", _agentState.AgentId, result);

                    await _llmHandler.CancelCurrentLLMTaskAsync();

                    var currentlyLeftToPlay = _audioOutput.CurrentlyLeftToPlay();
                    if (currentlyLeftToPlay > TimeSpan.Zero)
                    {
                        // todo can make 100 configurable by user maybe?
                        var amountToWait = 100;
                        await _audioOutput.StartVolumeFadeAsync(0.0f, TimeSpan.FromMilliseconds(amountToWait), cancellationToken);
                        await Task.Delay(amountToWait);
                        await _audioOutput.CancelCurrentSpeechPlaybackAsync();
                    }

                    _interruptViAILLMBuilder.Clear();
                    _interruptLLMTask = null;

                    // send the text to orchestrator instead try to todo
                    await _audioOutput.StartVolumeFadeAsync(1f, TimeSpan.FromMilliseconds(10), cancellationToken);
                    await _llmHandler.ProcessSystemMessageAsync($"Your response was interrupted by the customer.\n\nYou spoke the following:\n\n```\n{spokenSoFar}\n```\n\nTo which the customer replied:\n\n```\n{text}\n```", clientId, cancellationToken);
                    return;
                }

                if (isEndOfResponse)
                {
                    _agentState.InterruptingLLMService.ClearMessageStreamed();
                    _interruptViAILLMBuilder.Clear();
                    _interruptLLMTask = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {AgentId}: Error processing interrupting LLM stream.", _agentState.AgentId);
                _agentState.InterruptingLLMService.ClearMessageStreamed();
                _interruptViAILLMBuilder.Clear();
                _interruptLLMTask = null;
            }
        }

        // Disposing
        private void DisposeCurrentVadService()
        {
            if (_vadService != null)
            {
                _logger.LogDebug("Disposing VAD service for Agent {AgentId}.", _agentState.AgentId);
                try { _vadService.VoiceActivityChanged -= OnVoiceActivityChanged; } catch { }
                try { _vadService.Dispose(); } catch (Exception ex) { _logger.LogWarning(ex, "Exception disposing VAD service."); }
                _vadService = null;
                _agentState.VadService = null;
            }
        }
        private void DisposeCurrentInterruptionLLMService(IDisposable? service)
        {
            if (service == null) return;

            if (service is ILLMService llmService)
            {
                try
                {
                    llmService.ClearMessageStreamed();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Agent {AgentId}: Exception unsubscribing Interruption LLM CheckIfInterruptibleViaAIMessageStreamHandlerAsync.", _agentState.AgentId);
                }
            }

            try
            {
                service.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Agent {AgentId}: Exception disposing Interruption LLM service object.", _agentState.AgentId);
            }
        }
        public void Dispose()
        {
            CancelVADInterruptTimer();
            // TODO Cancel interrupting LLM task?
            _interruptLLMTask?.Wait(500);

            DisposeCurrentVadService();
            DisposeCurrentInterruptionLLMService(_agentState.InterruptingLLMService);
            _agentState.InterruptingLLMService = null;

            _vadInterruptTimerCTS?.Dispose();
            // Dispose interrupting LLM CTS? Needs careful management.
            _logger.LogDebug("Interruption Manager disposed for Agent {AgentId}.", _agentState.AgentId);
        }
    }
}