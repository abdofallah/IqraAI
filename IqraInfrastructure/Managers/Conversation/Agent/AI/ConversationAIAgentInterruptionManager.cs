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
using static System.Net.Mime.MediaTypeNames;


namespace IqraInfrastructure.Managers.Conversation.Agent.AI
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
        private Task? _interruptLLMTask = null;

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

                if (_agentState.CurrentConversationType == AgentInterruptionTypeENUM.InterruptibleViaAI && false)
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

            FunctionReturnResult<ILLMService?> interuptibleLLMServiceResult;
            if (((BusinessAppRouteAgentInterruptionViaAI)_agentState.CurrentSessionRoute?.Agent.Interruption).UseCurrentAgentLLMForInterrupting)
            {
                // Ensure primary LLM and its integration data are available
                if (_agentState.LLMService == null || _agentState.LLMBusinessIntegrationData == null || _agentState.BusinessAppAgent == null)
                {
                    _logger.LogError("Agent {AgentId}: Cannot reuse primary LLM for interruption - primary LLM not initialized.", _agentState.AgentId);
                    throw new InvalidOperationException("Primary LLM required for reuse in interruption.");
                }
                var defaultLLMServiceInfo = _agentState.BusinessAppAgent.Integrations.LLM[_agentState.CurrentLanguageCode][0];
                // TODO: Pass correct options if needed (e.g., lower latency settings?)
                interuptibleLLMServiceResult = await _llmProviderManager.BuildProviderServiceByIntegration(
                   _agentState.LLMBusinessIntegrationData,
                   defaultLLMServiceInfo,
                   new Dictionary<string, string> { } /* Add specific options? */
                 );
            }
            else
            {
                // TODO: Implement logic to build from a *different* configured integration
                _logger.LogError("Agent {AgentId}: Using a separate interrupting LLM is not yet implemented.", _agentState.AgentId);
                throw new NotImplementedException("Separate interrupting LLM configuration not implemented.");
            }

            if (!interuptibleLLMServiceResult.Success || interuptibleLLMServiceResult.Data == null)
            {
                _logger.LogError("Agent {AgentId}: Failed to build interrupting LLM service: {ErrorMessage}", _agentState.AgentId, interuptibleLLMServiceResult.Message);
                throw new InvalidOperationException($"Failed to build interrupting LLM service: {interuptibleLLMServiceResult.Message}");
            }

            DisposeCurrentService(_agentState.InterruptingLLMService); // Dispose old one
            _agentState.InterruptingLLMService = interuptibleLLMServiceResult.Data;
            // Set the specific system prompt for interruption decisions
            _agentState.InterruptingLLMService.SetSystemPrompt("Here are the guidelines for thinking before a response (for the thoughts of the model)\r\n<ThinkingGuidelines>\r\n\t- Only use thinking when required else give response directly.\r\n\t- The thinking text should always be summarized and not exceed more than 1 to 3 sentences.\r\n\t- The thinking text should always be less than maximum of 100 characters.\r\n</ThinkingGuidelines>\r\n\r\nYou will be given response of a customer support agent and the current spoken words/sentence by the customer. Decide whether the customer support agent should keep speaking their current sentence or be inntrupted by the customer to let the customer speak.\r\n\r\nIf we should let the customer speak, respond back with: \"allow_interrupt\".\r\nIf we should let the customer support agent speak, respond back with: \"continue_speaking\".");
            _logger.LogInformation("Agent {AgentId}: Interrupting LLM Initialized.", _agentState.AgentId);
        }
        public async Task ReInitializeForLanguageAsync(CancellationToken agentCTS) // Added token
        {
            _logger.LogInformation("Agent {AgentId}: Re-initializing Interruption Manager for new language.", _agentState.AgentId);

            if (_agentState.CurrentConversationType == AgentInterruptionTypeENUM.InterruptibleViaAI)
            {
                await InitializeInterruptingLLMAsync();
            }
        }

        // Called by Orchestrator's ProcessTextAsync when text arrives WHILE agent is speaking
        public async Task<bool> CheckCanInterruptAgentAsync(string text, string? clientId, CancellationToken externalToken)
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
                    if (((BusinessAppRouteAgentInterruptionTurnByTurn)_agentState.CurrentSessionRoute.Agent.Interruption).UseInterruptedResponseInNextTurn)
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
                var amountToWait = Math.Min(200, (int)_agentState.CurrentResponseDurationSpeakingStarted.Value.Add(_audioOutput.CurrentlyLeftToPlay()).Subtract(DateTime.UtcNow).TotalMilliseconds); 
                await _audioOutput.StartVolumeFadeAsync(0.0f, TimeSpan.FromMilliseconds(amountToWait));
                await Task.Delay(amountToWait);
                await _audioOutput.CancelCurrentSpeechPlaybackAsync();

                await _audioOutput.StartVolumeFadeAsync(1.0f, TimeSpan.FromMilliseconds(1));

                return false;
            }


            if (!_agentState.IsResponding
                &&
                (_agentState.CurrentConversationType == AgentInterruptionTypeENUM.InterruptibleViaVAD || _agentState.CurrentConversationType == AgentInterruptionTypeENUM.InterruptibleViaAI)
            )
            {
                return false;
            }

            // Agent is responding and user starts speaking, we will go ahead and buffer the text
            // we will let OnVoiceActivityChanged decide what to do
            if (_agentState.CurrentConversationType == AgentInterruptionTypeENUM.InterruptibleViaVAD)
            {
                _logger.LogDebug("Agent {AgentId}: Buffering text during VAD interrupt check: '{Text}'", _agentState.AgentId, text);
                _agentState.InterruptResponseBuffer.Append(text + " ");
                return true;
            }
  
            // todo check
            if (_agentState.CurrentConversationType == AgentInterruptionTypeENUM.InterruptibleViaAI)
            {
                if (_agentState.IsProcessingInterruption || _agentState.InterruptingLLMService == null)
                {
                    _logger.LogWarning("Agent {AgentId}: Ignoring text for AI interrupt - already processing or service unavailable.", _agentState.AgentId);
                    _agentState.InterruptResponseBuffer.Append(text + " ");
                    return true;
                }

                // --- Start AI Interruption Check ---
                _logger.LogInformation("Agent {AgentId}: Starting AI interruption check for text: '{Text}'", _agentState.AgentId, text);
                _agentState.IsProcessingInterruption = true;
                _agentState.InterruptResponseBuffer.Clear(); // Clear buffer for this check


                // TODO: Determine the 'spokenSoFar' accurately. This is hard!
                // Need coordination with AudioOutput timing. For now, use a rough estimate or simplified logic.
                // Original code used timing estimation. Let's try that.
                string spokenSoFar = "Estimating spoken part..."; // Placeholder - requires better implementation
                if (_agentState.CurrentResponseDurationSpeakingStarted.HasValue && _agentState.CurrentResponseDuration > TimeSpan.Zero)
                {
                    TimeSpan elapsedTime = DateTime.UtcNow - _agentState.CurrentResponseDurationSpeakingStarted.Value;
                    string fullResponseText = "";//_llmHandler.GetCurrentResponseText(); // Need method in LLMHandler
                    double proportionSpoken = Math.Clamp(elapsedTime.TotalSeconds / _agentState.CurrentResponseDuration.TotalSeconds, 0.0, 1.0);
                    int spokenLength = (int)(fullResponseText.Length * proportionSpoken);
                    spokenSoFar = fullResponseText.Substring(0, Math.Min(spokenLength, fullResponseText.Length));
                    _logger.LogDebug("Agent {AgentId}: Estimated spoken so far ({Proportion:P1}): {Text}", _agentState.AgentId, proportionSpoken, spokenSoFar);
                }
                else
                {
                    _logger.LogWarning("Agent {AgentId}: Could not estimate spoken part for AI interrupt check.", _agentState.AgentId);
                }


                _agentState.InterruptingLLMService.ClearMessages();
                _agentState.InterruptingLLMService.AddUserMessage($"current agent response: {spokenSoFar}\ncurrent overlaping customer response: {text}");

                // Subscribe to the interrupting LLM stream locally
                // TODO: This assumes the interrupting LLM also streams. Adjust if not.
                // Need to handle unsubscribing carefully.
                Action<object?, object> streamHandler = null!; // Declare variable first
                streamHandler = async (sender, responseObj) => {
                    await CheckIfInterruptibleViaAIMessageStreamHandlerAsync(sender, responseObj, spokenSoFar, text, clientId, externalToken, streamHandler);
                };
                //_agentState.InterruptingLLMService?.MessageStreamed += streamHandler; TODO REMOVED


                // Combine CTS: agent shutdown, external call token
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(_agentState.MasterCancellationToken, externalToken); // Need agent master CTS access

                _interruptLLMTask = _agentState.InterruptingLLMService.ProcessInputAsync(combinedCts.Token);
                // Don't await here, let the stream handler manage completion

                return true; // Indicate text is being handled by interruption logic
            }

            return false; // Should not reach here
        }

        // Called by Orchestrator/AudioInput when VAD event occurs
        private async void OnVoiceActivityChanged(object? sender, VadEventArgs e)
        {
            _logger.LogInformation("Agent {AgentId}: VAD activity changed: SpeechDetected={IsSpeechDetected}", _agentState.AgentId, e.IsSpeechDetected);
            _agentState.IsUserSpeakingVAD = e.IsSpeechDetected;

            if (!_agentState.IsResponding)
            {
                _agentState.UserSpeechStartTime = null;
                CancelVADInterruptTimer();
                _agentState.InterruptResponseBuffer.Clear();

                return;
            }

            // Agent is responding and user starts speaking
            if (e.IsSpeechDetected) 
            {
                _agentState.UserSpeechStartTime = DateTime.UtcNow;
                _agentState.InterruptResponseBuffer.Clear();

                if (_agentState.CurrentConversationType == AgentInterruptionTypeENUM.InterruptibleViaVAD)
                {
                    int interruptMs = ((BusinessAppRouteAgentInterruptionViaVAD)_agentState.CurrentSessionRoute?.Agent.Interruption).InterruptibleConversationAudioActivityDurationMS;
                    _logger.LogInformation("Agent {AgentId}: VAD Speech Start (VAD Interrupt) - Fading out agent over {ms}ms and starting timer.", _agentState.AgentId, interruptMs);
                    await _audioOutput.StartVolumeFadeAsync(0.1f, TimeSpan.FromMilliseconds(interruptMs));
                    StartVADInterruptTimer(TimeSpan.FromMilliseconds(interruptMs));
                }
            }
            // Agent is responding and user stopped speaking
            else 
            {
                _agentState.UserSpeechStartTime = null;
                CancelVADInterruptTimer(); // Stop timer if user stops talking

                // todo check if _agentState.IsProcessingInterruption is needed here
                if (_agentState.CurrentAgentVolumeFactor < 1.0f && !_agentState.IsProcessingInterruption)
                {
                    _logger.LogInformation("Agent {AgentId}: VAD Silence - Fading agent back in.", _agentState.AgentId);
                    await _audioOutput.StartVolumeFadeAsync(1.0f, TimeSpan.FromMilliseconds(200));
                }
            }
        }

        private void StartVADInterruptTimer(TimeSpan interruptDelay)
        {
            CancelVADInterruptTimer();

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

                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    // User is still speaking after the given interrupt delay but remember agent is still responding as well
                    if (_agentState.IsUserSpeakingVAD && _agentState.IsResponding)
                    {
                        _logger.LogInformation("Agent {AgentId}: VAD interrupt timer expired while user speaking while agent is speaking. Triggering interrupt.", _agentState.AgentId);

                        await _llmHandler.CancelCurrentLLMTaskAsync();

                        // todo can make 50 configurable by user maybe?
                        var amountToWait = Math.Min(200, (int)_agentState.CurrentResponseDurationSpeakingStarted.Value.Add(_audioOutput.CurrentlyLeftToPlay()).Subtract(DateTime.UtcNow).TotalMilliseconds);
                        await _audioOutput.StartVolumeFadeAsync(0.0f, TimeSpan.FromMilliseconds(amountToWait));
                        await Task.Delay(amountToWait);
                        await _audioOutput.CancelCurrentSpeechPlaybackAsync();

                        await _audioOutput.StartVolumeFadeAsync(1.0f, TimeSpan.FromMilliseconds(50));

                        if (!_agentState.IsSTTRecognizing && _agentState.InterruptResponseBuffer.Length > 0)
                        {
                            string text = _agentState.InterruptResponseBuffer.ToString();
                            _agentState.InterruptResponseBuffer.Clear();
                            await _llmHandler.ProcessUserTextAsync(text, _agentState.CurrentClientId, _agentState.MasterCancellationToken);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogTrace("Agent {AgentId}: VAD interrupt timer task cancelled.", _agentState.AgentId);
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
                }
            }, token);
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

        // TODO check
        private async Task CheckIfInterruptibleViaAIMessageStreamHandlerAsync(
            object? sender,
            object responseObj,
            string spokenSoFarSnapshot, // Pass snapshot
            string customerOverlapText,
            string? clientId,
            CancellationToken callerToken,
            Action<object?, object> selfHandler // Pass handler to allow unsubscribing
            )
        {
            //// --- Move logic from original CheckIfInterruptible here ---
            //// Extract chunk, append to _agentState.InterruptResponseBuffer
            //// On end of stream:
            ////   - Unsubscribe selfHandler
            ////   - Check result ("allow_interrupt" or "continue_speaking")
            ////   - If allow_interrupt:
            ////      - Call TriggerImmediateInterruptAsync
            ////      - Process customerOverlapText via _llmHandler
            ////   - If continue_speaking:
            ////      - Fade back in via _audioOutput.StartVolumeFadeAsync
            ////   - Reset _agentState.IsProcessingInterruption

            //if (_agentState.InterruptingLLMService == null) return; // Guard

            //try
            //{
            //    FunctionReturnResult<(string? deltaText, bool isEndOfResponse)?> chunkExtractResult = LLMStreamingChunkDataExtractHelper.GetChunkData(responseObj, _agentState.InterruptingLLMService.GetProviderType());
            //    if (!chunkExtractResult.Success || !chunkExtractResult.Data.HasValue)
            //    {
            //        _logger.LogError("Agent {AgentId}: Error extracting interrupting LLM chunk: {Reason}", _agentState.AgentId, chunkExtractResult.Message);
            //        return;
            //    }
            //    (string? deltaText, bool isEndOfResponse) = chunkExtractResult.Data.Value;

            //    if (!string.IsNullOrEmpty(deltaText))
            //    {
            //        _agentState.InterruptResponseBuffer.Append(deltaText);
            //    }

            //    if (isEndOfResponse)
            //    {
            //        // --- Critical: Unsubscribe from the event ---
            //        if (sender is ILLMService service)
            //        {
            //            //service.MessageStreamed -= selfHandler; TODO removed
            //            _logger.LogTrace("Agent {AgentId}: Unsubscribed from interrupting LLM stream.", _agentState.AgentId);
            //        }
            //        else
            //        {
            //            _logger.LogWarning("Agent {AgentId}: Could not unsubscribe from interrupting LLM stream - sender mismatch.", _agentState.AgentId);
            //        }


            //        string result = _agentState.InterruptResponseBuffer.ToString().ToLower().Trim();
            //        _logger.LogInformation("Agent {AgentId}: AI Interrupt check result: '{Result}'", _agentState.AgentId, result);

            //        if (result.Contains("allow_interrupt"))
            //        {
            //            _logger.LogInformation("Agent {AgentId}: AI decided to allow interrupt.", _agentState.AgentId);
            //            // 1. Trigger the interruption actions (stop speech, etc.)
            //            await TriggerImmediateInterruptAsync(spokenSoFarSnapshot, "AI Decision"); // Pass snapshot

            //            // 2. Process the text that caused the interruption
            //            _logger.LogDebug("Agent {AgentId}: Processing customer text after AI allowed interrupt.", _agentState.AgentId);
            //            // Use CancellationToken.None or link appropriately? None is safer.
            //            await _llmHandler.ProcessUserTextAsync(customerOverlapText, clientId, CancellationToken.None);
            //        }
            //        else // continue_speaking or unexpected result
            //        {
            //            if (!result.Contains("continue_speaking"))
            //            {
            //                _logger.LogWarning("Agent {AgentId}: Unexpected result from interrupting LLM: {Result}", _agentState.AgentId, result);
            //            }
            //            _logger.LogInformation("Agent {AgentId}: AI decided to continue speaking. Fading back in.", _agentState.AgentId);
            //            await _audioOutput.StartVolumeFadeAsync(1.0f, TimeSpan.FromMilliseconds(200)); // Quick fade in
            //        }

            //        // Reset interruption state
            //        _agentState.IsProcessingInterruption = false;
            //        _agentState.InterruptResponseBuffer.Clear();
            //        _interruptLLMTask = null;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Agent {AgentId}: Error processing interrupting LLM stream.", _agentState.AgentId);
            //    // Ensure state is reset even on error
            //    _agentState.IsProcessingInterruption = false;
            //    _agentState.InterruptResponseBuffer.Clear();
            //    _interruptLLMTask = null;
            //    // Try to unsubscribe just in case
            //    //if (sender is ILLMService service) { service.MessageStreamed -= selfHandler; } TODO REMOVED
            //}
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
        private void DisposeCurrentService(IDisposable? service)
        {
            // Copied from LLM Handler - make a shared utility?
            if (service == null) return;
            // Unsubscribe specific events if needed
            try
            {
                service.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Agent {AgentId}: Exception disposing service object.", _agentState.AgentId);
            }
        }
        public void Dispose()
        {
            CancelVADInterruptTimer();
            // Cancel interrupting LLM task?
            // _interruptLLMTask?.Wait(TimeSpan.FromSeconds(1)); // Careful with waiting on dispose

            DisposeCurrentVadService();
            DisposeCurrentService(_agentState.InterruptingLLMService);
            _agentState.InterruptingLLMService = null;

            _vadInterruptTimerCTS?.Dispose();
            // Dispose interrupting LLM CTS? Needs careful management.
            _logger.LogDebug("Interruption Manager disposed for Agent {AgentId}.", _agentState.AgentId);
        }
    }
}