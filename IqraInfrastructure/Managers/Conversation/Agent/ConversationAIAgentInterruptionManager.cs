using IqraCore.Interfaces.AI; // For ILLMService
using IqraCore.Interfaces.VAD;
using IqraInfrastructure.Managers.LLM; // For LLMProviderManager
using IqraInfrastructure.Managers.LLM.Providers.Helpers; // For chunk helper
using Microsoft.Extensions.Logging;
using System;
using System.Text; // For StringBuilder
using System.Threading;
using System.Threading.Tasks;
using IqraInfrastructure.Managers.VAD; // For SileroVadService concrete type if needed


namespace IqraInfrastructure.Managers.Conversation.Modules
{
    public class ConversationAIAgentInterruptionManager : IDisposable
    {
        // Dependencies
        private readonly ILogger<ConversationAIAgentInterruptionManager> _logger;
        private readonly ConversationAIAgentState _agentState;
        private readonly LLMProviderManager _llmProviderManager; // To build interrupting LLM if needed
        private readonly BusinessManager _businessManager; // Might be needed for interrupting LLM integration data

        // References to other modules for coordination
        private readonly ConversationAIAgentAudioOutput _audioOutput;
        private readonly ConversationAIAgentLLMHandler _llmHandler; // To cancel main LLM task, process text after interrupt

        // VAD State & Timer
        private CancellationTokenSource? _vadInterruptTimerCTS = null;
        private Task? _vadInterruptTimerTask = null;
        // _agentState.UserSpeechStartTime used for timing

        // Interrupting LLM State
        private Task? _interruptLLMTask = null;
        // _agentState.InterruptResponseBuffer used for accumulation
        // _agentState.IsProcessingInterruption flag

        // Buffered Text during VAD attempt
        private StringBuilder _bufferedInterruptText = new StringBuilder(); // Store text received during potential VAD interrupt

        // VAD service managed here
        private IVadService? _vadService; // Use _agentState.VadService instead? Let's manage here for now.
        private VadOptions? _vadOptions; // Use _agentState.VadOptions?


        public ConversationAIAgentInterruptionManager(
            ILoggerFactory loggerFactory,
            ConversationAIAgentState agentState,
            LLMProviderManager llmProviderManager,
            BusinessManager businessManager,
            ConversationAIAgentAudioOutput audioOutput, // Inject refs to modules needed
            ConversationAIAgentLLMHandler llmHandler)
        {
            _logger = loggerFactory.CreateLogger<ConversationAIAgentInterruptionManager>();
            _agentState = agentState;
            _llmProviderManager = llmProviderManager;
            _businessManager = businessManager;
            _audioOutput = audioOutput;
            _llmHandler = llmHandler;
        }

        public async Task InitializeAsync(CancellationToken agentCTS)
        {
            // --- Move VAD initialization logic from InitalizeVAD ---
            if (_agentState.CurrentConversationType != AgentConversationTypeENUM.TurnByTurn)
            {
                _logger.LogInformation("Agent {AgentId}: Initializing VAD for interruptible conversation.", _agentState.AgentId);
                _vadOptions = new VadOptions { SampleRate = 16000 }; // TODO: Configurable? SampleRate from AudioOutput const?
                                                                     // TODO: Make model path configurable
                string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VadModels\\silero_vad.onnx");
                if (!File.Exists(modelPath))
                {
                    _logger.LogError("Agent {AgentId}: VAD model file not found at {Path}", _agentState.AgentId, modelPath);
                    // Decide how to handle - throw, or disable interruption? Disable for now.
                    _agentState.CurrentConversationType = AgentConversationTypeENUM.TurnByTurn; // Fallback
                    _logger.LogWarning("Agent {AgentId}: Falling back to TurnByTurn mode due to missing VAD model.", _agentState.AgentId);
                }
                else
                {
                    DisposeCurrentVadService(); // Dispose previous if any
                                                // Use concrete type if Initialize requires specific params not on interface
                    _vadService = new SileroVadService(LoggerFactory.Create(b => b.AddConsole()).CreateLogger<SileroVadService>()); // Provide logger properly
                    _vadService.Initialize(modelPath, _vadOptions);
                    _vadService.VoiceActivityChanged += OnVoiceActivityChanged;
                    _agentState.VadService = _vadService; // Store in state if needed by AudioInput
                    _logger.LogInformation("Agent {AgentId}: VAD Initialized.", _agentState.AgentId);
                }
            }
            else
            {
                _logger.LogInformation("Agent {AgentId}: VAD not required for TurnByTurn conversation.", _agentState.AgentId);
            }


            // --- Initialize Interrupting LLM if needed ---
            if (_agentState.CurrentConversationType == AgentConversationTypeENUM.InterruptibleViaAI)
            {
                // This logic is complex, ensure it matches original InitializeLLMForLanguage part
                await InitializeInterruptingLLMAsync(); // Extracted logic
            }

            _logger.LogInformation("Interruption Manager initialized for Agent {AgentId}.", _agentState.AgentId);
        }

        private async Task InitializeInterruptingLLMAsync()
        {
            // --- Move Interrupting LLM setup logic here ---
            // Get config from _agentState.CurrentSessionRoute.Agent.UseCurrentAgentLLMForInterrupting
            // Build service using _llmProviderManager / _businessManager
            // Store in _agentState.InterruptingLLMService
            // Set system prompt

            _logger.LogDebug("Agent {AgentId}: Setting up interrupting LLM.", _agentState.AgentId);

            FunctionReturnResult<ILLMService?> interuptibleLLMServiceResult;
            if (_agentState.CurrentSessionRoute?.Agent.UseCurrentAgentLLMForInterrupting == true)
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
            // VAD usually doesn't need language change
            // Interrupting LLM might need re-initialization if language affects model choice/prompt
            if (_agentState.CurrentConversationType == AgentConversationTypeENUM.InterruptibleViaAI)
            {
                await InitializeInterruptingLLMAsync();
            }
        }


        // Called by Orchestrator/AudioInput when VAD event occurs
        private async void OnVoiceActivityChanged(object? sender, VadEventArgs e)
        {
            // --- Move logic from original OnVoiceActivityChanged here ---
            // Update _agentState.IsUserSpeakingVAD
            // Check _agentState.IsResponding
            // Based on _agentState.CurrentConversationType:
            // - Start/stop volume fade via _audioOutput.StartVolumeFadeAsync
            // - Start/stop VAD timer (StartVADInterruptTimer / CancelVADInterruptTimer)
            // - Handle fade-in/out logic

            _logger.LogDebug("Agent {AgentId}: VAD activity changed: SpeechDetected={IsSpeechDetected}", _agentState.AgentId, e.IsSpeechDetected);
            _agentState.IsUserSpeakingVAD = e.IsSpeechDetected;

            if (!_agentState.IsResponding) // Agent is not speaking
            {
                if (!_agentState.IsUserSpeakingVAD && _agentState.CurrentAgentVolumeFactor < 1.0f)
                {
                    // User stopped speaking while agent was silent, ensure agent volume is back to normal
                    _logger.LogTrace("Agent {AgentId}: VAD silence while agent silent, ensuring volume is 1.0.", _agentState.AgentId);
                    await _audioOutput.StartVolumeFadeAsync(1.0f, TimeSpan.FromMilliseconds(100)); // Fast fade in
                }
                _agentState.UserSpeechStartTime = null;
                CancelVADInterruptTimer(); // Ensure timer is stopped if agent isn't speaking
                _bufferedInterruptText.Clear(); // Clear any buffered text if user stops talking
                return;
            }

            // --- Agent IS Responding ---
            if (e.IsSpeechDetected) // User started speaking
            {
                _agentState.UserSpeechStartTime = DateTime.UtcNow;
                _bufferedInterruptText.Clear(); // Clear buffer on new speech start

                if (_agentState.CurrentConversationType == AgentConversationTypeENUM.InterruptibleViaAI)
                {
                    // Fade out moderately quickly while AI decides
                    _logger.LogDebug("Agent {AgentId}: VAD Speech Start (AI Interrupt) - Fading out agent.", _agentState.AgentId);
                    await _audioOutput.StartVolumeFadeAsync(0.1f, TimeSpan.FromMilliseconds(500)); // Example: fade to 10% over 0.5s
                                                                                                   // The actual AI check happens when text arrives via HandlePotentialInterruption
                }
                else if (_agentState.CurrentConversationType == AgentConversationTypeENUM.InterruptibleViaVAD)
                {
                    int interruptMs = _agentState.CurrentSessionRoute?.Agent.InterruptibleConversationAudioActivityDurationMS ?? 1000; // Default 1s
                    _logger.LogDebug("Agent {AgentId}: VAD Speech Start (VAD Interrupt) - Fading out agent over {ms}ms and starting timer.", _agentState.AgentId, interruptMs);
                    await _audioOutput.StartVolumeFadeAsync(0.1f, TimeSpan.FromMilliseconds(interruptMs)); // Fade out over the timer duration
                    StartVADInterruptTimer(TimeSpan.FromMilliseconds(interruptMs));
                }
                // TurnByTurn: No action needed on speech start while agent is speaking
            }
            else // User stopped speaking
            {
                _agentState.UserSpeechStartTime = null;
                CancelVADInterruptTimer(); // Stop timer if user stops talking

                // If agent was faded out and no interruption is being processed (by AI LLM), fade back in
                if (_agentState.CurrentAgentVolumeFactor < 1.0f && !_agentState.IsProcessingInterruption)
                {
                    _logger.LogDebug("Agent {AgentId}: VAD Silence - Fading agent back in.", _agentState.AgentId);
                    await _audioOutput.StartVolumeFadeAsync(1.0f, TimeSpan.FromMilliseconds(200)); // Quick fade in
                }

                // Process any text buffered during the VAD speech segment
                if (_bufferedInterruptText.Length > 0)
                {
                    _logger.LogInformation("Agent {AgentId}: Processing text buffered during VAD silence: {Text}", _agentState.AgentId, _bufferedInterruptText.ToString());
                    var textToProcess = _bufferedInterruptText.ToString();
                    _bufferedInterruptText.Clear();
                    // Use a CancellationToken.None or link to agentCTS? None seems safer here.
                    await _llmHandler.ProcessUserTextAsync(textToProcess, _agentState.CurrentClientId, CancellationToken.None);
                }

            }
        }

        // Called by Orchestrator's ProcessTextAsync when text arrives WHILE agent is speaking
        public async Task<bool> HandlePotentialInterruptionAsync(string text, string? clientId, CancellationToken externalToken)
        {
            if (!_agentState.IsResponding || _agentState.CurrentConversationType == AgentConversationTypeENUM.TurnByTurn)
            {
                return false; // Not interrupting or not interruptible
            }

            if (_agentState.CurrentConversationType == AgentConversationTypeENUM.InterruptibleViaVAD)
            {
                // Buffer the text. If VAD timer expires, it will trigger full interrupt.
                // If user stops talking before timer, OnVoiceActivityChanged handles sending buffered text.
                _logger.LogDebug("Agent {AgentId}: Buffering text during VAD interrupt check: '{Text}'", _agentState.AgentId, text);
                _bufferedInterruptText.Append(text + " "); // Add space between chunks
                return true; // Indicate that text was handled (buffered) by interruption logic
            }
            else if (_agentState.CurrentConversationType == AgentConversationTypeENUM.InterruptibleViaAI)
            {
                if (_agentState.IsProcessingInterruption || _agentState.InterruptingLLMService == null)
                {
                    _logger.LogWarning("Agent {AgentId}: Ignoring text for AI interrupt - already processing or service unavailable.", _agentState.AgentId);
                    // TODO: Buffer this text too? Or just drop? Buffering seems safer.
                    _bufferedInterruptText.Append(text + " ");
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
                    string fullResponseText = _llmHandler.GetCurrentResponseText(); // Need method in LLMHandler
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
                    await CheckIfInterruptibleStreamHandlerAsync(sender, responseObj, spokenSoFar, text, clientId, externalToken, streamHandler);
                };
                _agentState.InterruptingLLMService.MessageStreamed += streamHandler;


                // Combine CTS: agent shutdown, external call token
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(_agentState.AgentConfiguration!.GetMasterCancellationToken(), externalToken); // Need agent master CTS access

                _interruptLLMTask = _agentState.InterruptingLLMService.ProcessInputAsync(combinedCts.Token);
                // Don't await here, let the stream handler manage completion

                return true; // Indicate text is being handled by interruption logic
            }

            return false; // Should not reach here
        }

        private async Task CheckIfInterruptibleStreamHandlerAsync(
            object? sender,
            object responseObj,
            string spokenSoFarSnapshot, // Pass snapshot
            string customerOverlapText,
            string? clientId,
            CancellationToken callerToken,
            Action<object?, object> selfHandler // Pass handler to allow unsubscribing
            )
        {
            // --- Move logic from original CheckIfInterruptible here ---
            // Extract chunk, append to _agentState.InterruptResponseBuffer
            // On end of stream:
            //   - Unsubscribe selfHandler
            //   - Check result ("allow_interrupt" or "continue_speaking")
            //   - If allow_interrupt:
            //      - Call TriggerImmediateInterruptAsync
            //      - Process customerOverlapText via _llmHandler
            //   - If continue_speaking:
            //      - Fade back in via _audioOutput.StartVolumeFadeAsync
            //   - Reset _agentState.IsProcessingInterruption

            if (_agentState.InterruptingLLMService == null) return; // Guard

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
                    _agentState.InterruptResponseBuffer.Append(deltaText);
                }

                if (isEndOfResponse)
                {
                    // --- Critical: Unsubscribe from the event ---
                    if (sender is ILLMService service)
                    {
                        service.MessageStreamed -= selfHandler;
                        _logger.LogTrace("Agent {AgentId}: Unsubscribed from interrupting LLM stream.", _agentState.AgentId);
                    }
                    else
                    {
                        _logger.LogWarning("Agent {AgentId}: Could not unsubscribe from interrupting LLM stream - sender mismatch.", _agentState.AgentId);
                    }


                    string result = _agentState.InterruptResponseBuffer.ToString().ToLower().Trim();
                    _logger.LogInformation("Agent {AgentId}: AI Interrupt check result: '{Result}'", _agentState.AgentId, result);

                    if (result.Contains("allow_interrupt"))
                    {
                        _logger.LogInformation("Agent {AgentId}: AI decided to allow interrupt.", _agentState.AgentId);
                        // 1. Trigger the interruption actions (stop speech, etc.)
                        await TriggerImmediateInterruptAsync(spokenSoFarSnapshot, "AI Decision"); // Pass snapshot

                        // 2. Process the text that caused the interruption
                        _logger.LogDebug("Agent {AgentId}: Processing customer text after AI allowed interrupt.", _agentState.AgentId);
                        // Use CancellationToken.None or link appropriately? None is safer.
                        await _llmHandler.ProcessUserTextAsync(customerOverlapText, clientId, CancellationToken.None);
                    }
                    else // continue_speaking or unexpected result
                    {
                        if (!result.Contains("continue_speaking"))
                        {
                            _logger.LogWarning("Agent {AgentId}: Unexpected result from interrupting LLM: {Result}", _agentState.AgentId, result);
                        }
                        _logger.LogInformation("Agent {AgentId}: AI decided to continue speaking. Fading back in.", _agentState.AgentId);
                        await _audioOutput.StartVolumeFadeAsync(1.0f, TimeSpan.FromMilliseconds(200)); // Quick fade in
                    }

                    // Reset interruption state
                    _agentState.IsProcessingInterruption = false;
                    _agentState.InterruptResponseBuffer.Clear();
                    _interruptLLMTask = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {AgentId}: Error processing interrupting LLM stream.", _agentState.AgentId);
                // Ensure state is reset even on error
                _agentState.IsProcessingInterruption = false;
                _agentState.InterruptResponseBuffer.Clear();
                _interruptLLMTask = null;
                // Try to unsubscribe just in case
                if (sender is ILLMService service) { service.MessageStreamed -= selfHandler; }
            }
        }


        private void StartVADInterruptTimer(TimeSpan interruptDelay)
        {
            // --- Move logic from original StartVADInterruptTimer here ---
            CancelVADInterruptTimer(); // Cancel previous first

            _vadInterruptTimerCTS = new CancellationTokenSource();
            var token = _vadInterruptTimerCTS.Token;

            _logger.LogDebug("Agent {AgentId}: Starting VAD interrupt timer for {Delay}.", _agentState.AgentId, interruptDelay);

            _vadInterruptTimerTask = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(interruptDelay, token);

                    // Timer completed without cancellation
                    if (!token.IsCancellationRequested && _agentState.IsUserSpeakingVAD && _agentState.IsResponding)
                    {
                        _logger.LogInformation("Agent {AgentId}: VAD interrupt timer expired while user speaking. Triggering interrupt.", _agentState.AgentId);

                        // Fade out completely and quickly
                        await _audioOutput.StartVolumeFadeAsync(0.0f, TimeSpan.FromMilliseconds(50));

                        // Trigger the interruption actions
                        string spokenSoFar = "Estimating spoken part..."; // Placeholder - requires better implementation
                                                                          // Add same estimation logic as AI interrupt if possible/needed
                        await TriggerImmediateInterruptAsync(spokenSoFar, "VAD Timer Expired");

                        // Process any buffered text after interruption
                        if (_bufferedInterruptText.Length > 0)
                        {
                            _logger.LogInformation("Agent {AgentId}: Processing text buffered during VAD interrupt: {Text}", _agentState.AgentId, _bufferedInterruptText.ToString());
                            var textToProcess = _bufferedInterruptText.ToString();
                            _bufferedInterruptText.Clear();
                            // Use CancellationToken.None?
                            await _llmHandler.ProcessUserTextAsync(textToProcess, _agentState.CurrentClientId, CancellationToken.None);
                        }
                    }
                    else if (token.IsCancellationRequested)
                    {
                        _logger.LogTrace("Agent {AgentId}: VAD interrupt timer cancelled before expiry.", _agentState.AgentId);
                    }
                    else if (!_agentState.IsUserSpeakingVAD)
                    {
                        _logger.LogTrace("Agent {AgentId}: VAD interrupt timer expired, but user stopped speaking.", _agentState.AgentId);
                        // User stopped, text should have been handled by OnVoiceActivityChanged
                    }
                    // else: Agent stopped responding before timer expired - handled by normal flow/cancellation
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
            }, token); // Pass token to Task.Run
        }

        private void CancelVADInterruptTimer()
        {
            // --- Move logic from original CancelVADInterruptTimer here ---
            if (_vadInterruptTimerCTS != null && !_vadInterruptTimerCTS.IsCancellationRequested)
            {
                _logger.LogTrace("Agent {AgentId}: Cancelling VAD interrupt timer.", _agentState.AgentId);
                _vadInterruptTimerCTS.Cancel();
                // Don't dispose here, let the task finish and dispose it
            }
        }

        // Central method to perform the actions needed when an interrupt is confirmed
        private async Task TriggerImmediateInterruptAsync(string spokenSoFar, string reason)
        {
            _logger.LogInformation("Agent {AgentId}: Interrupt triggered. Reason: {Reason}", _agentState.AgentId, reason);

            // 1. Cancel the main LLM response generation
            await _llmHandler.CancelCurrentLLMTaskAsync();

            // 2. Cancel the current speech playback in AudioOutput
            await _audioOutput.CancelCurrentSpeechPlaybackAsync(); // This should also stop TTS

            // 3. Log the interrupted response in LLM history (optional but good practice)
            // Need to get the *intended* full response text from LLMHandler
            string fullResponseText = _llmHandler.GetCurrentResponseText(); // Requires method in LLMHandler
            if (!string.IsNullOrEmpty(fullResponseText))
            {
                var modifiedResponse = spokenSoFar + $"......(interrupted by customer via {reason} at this point but expected to speak) " + fullResponseText.Substring(Math.Min(spokenSoFar.Length, fullResponseText.Length));
                _logger.LogDebug("AI Agent {AgentId} adding interrupted assistant message: {Response}", _agentState.AgentId, modifiedResponse);
                // Add this modified message to the *primary* LLM's history
                _agentState.LLMService?.AddAssistantMessage(modifiedResponse);
            }


            // 4. Reset state flags (redundant if CancelCurrentLLMTaskAsync does it, but safe)
            _agentState.IsResponding = false;
            _agentState.IsExecutingSystemTool = false;
            _agentState.IsExecutingCustomTool = false;
            _agentState.IsProcessingInterruption = false; // Ensure this is reset too
            _bufferedInterruptText.Clear(); // Clear any remaining buffer

            // 5. Ensure VAD state is reset if needed (e.g., user might stop talking right after interrupting)
            _agentState.IsUserSpeakingVAD = false; // Assume user might stop right after

            // 6. Ensure volume is ready for potential user speech or next agent response
            await _audioOutput.StartVolumeFadeAsync(1.0f, TimeSpan.FromMilliseconds(50)); // Ensure volume is back up quickly

            _logger.LogInformation("Agent {AgentId}: Interrupt processing complete.", _agentState.AgentId);
        }


        public void ProcessVadAudio(ReadOnlyMemory<byte> audio)
        {
            _vadService?.ProcessAudio(audio);
        }


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