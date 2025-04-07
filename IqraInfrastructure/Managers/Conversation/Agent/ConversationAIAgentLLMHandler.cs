using IqraCore.Interfaces.AI;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.LLM.Providers.Helpers;
using IqraInfrastructure.Managers.Business; // If needed
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text; // For StringBuilder manipulation

namespace IqraInfrastructure.Managers.Conversation.Modules
{
    public class ConversationAIAgentLLMHandler : IDisposable
    {
        // Events for Orchestrator
        public event Func<string, Task>? SynthesizeTextRequested; // Request TTS
        public event Action<string>? TextChunkGenerated; // Notify partial text (e.g. for UI)
        public event Action? ResponseHandlingComplete; // Notify speaking finished
        public event Func<string, Task>? SystemToolExecutionRequested; // Request tool execution
        public event Func<string, Task>? CustomToolExecutionRequested; // Request tool execution

        private readonly ILogger<ConversationAIAgentLLMHandler> _logger;
        private readonly ConversationAIAgentState _agentState;
        private readonly LLMProviderManager _llmProviderManager;
        private readonly BusinessManager _businessManager;
        private readonly SystemPromptGenerator _systemPromptGenerator; // To regenerate prompts

        private readonly SemaphoreSlim _llmResponseLock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _currentLLMProcessingTaskCTS = new(); // CTS for the active LLM call
        private Task? _llmTask; // The active _llmService.ProcessInputAsync() task

        // Buffers managed here now
        private readonly StringBuilder _responseBuffer = new StringBuilder();
        private int _currentResponseBufferRead = 0;

        public ConversationAIAgentLLMHandler(
            ILoggerFactory loggerFactory,
            ConversationAIAgentState agentState,
            LLMProviderManager llmProviderManager,
            BusinessManager businessManager,
            SystemPromptGenerator systemPromptGenerator)
        {
            _logger = loggerFactory.CreateLogger<ConversationAIAgentLLMHandler>();
            _agentState = agentState;
            _llmProviderManager = llmProviderManager;
            _businessManager = businessManager;
            _systemPromptGenerator = systemPromptGenerator;
        }

        public async Task InitializeAsync()
        {
            // --- Move logic from original InitalizeLLMForLangauge here ---
            // Build _llmService using _llmProviderManager and store in _agentState
            // Build _interruptingLLMService if needed and store in _agentState
            // Generate _llmBaseSystemPrompt using _systemPromptGenerator and store in _agentState
            // Perform LLM warmup
            // Set initial system prompt on _llmService
            // Subscribe to _llmService.MessageStreamed += OnLLMMessageStreamed;

            _logger.LogInformation("LLM Handler initializing for Agent {AgentId}.", _agentState.AgentId);

            if (_agentState.BusinessAppAgent == null || string.IsNullOrEmpty(_agentState.CurrentLanguageCode) || _agentState.AgentConfiguration == null)
            {
                _logger.LogError("Agent {AgentId}: Cannot initialize LLM Handler - missing required state.", _agentState.AgentId);
                throw new InvalidOperationException("LLM Handler requires BusinessAppAgent, LanguageCode, and AgentConfiguration in state.");
            }

            // --- Build Primary LLM ---
            var defaultLLMServiceInfo = _agentState.BusinessAppAgent.Integrations.LLM[_agentState.CurrentLanguageCode][0];
            var llmBusinessIntegrationDataResult = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(_agentState.AgentConfiguration.BusinessId, defaultLLMServiceInfo.Id);
            if (!llmBusinessIntegrationDataResult.Success || llmBusinessIntegrationDataResult.Data == null)
            {
                _logger.LogError("Agent {AgentId}: Business app LLM integration {IntegrationId} not found", _agentState.AgentId, defaultLLMServiceInfo.Id);
                throw new InvalidOperationException($"Business app LLM integration {defaultLLMServiceInfo.Id} not found");
            }
            _agentState.LLMBusinessIntegrationData = llmBusinessIntegrationDataResult.Data;

            var llmServiceResult = await _llmProviderManager.BuildProviderServiceByIntegration(_agentState.LLMBusinessIntegrationData, defaultLLMServiceInfo, new Dictionary<string, string> { });
            if (!llmServiceResult.Success || llmServiceResult.Data == null)
            {
                _logger.LogError("Agent {AgentId}: Failed to build LLM service with error: {ErrorMessage}", _agentState.AgentId, llmServiceResult.Message);
                throw new InvalidOperationException($"Failed to build LLM service: {llmServiceResult.Message}");
            }

            DisposeCurrentService(_agentState.LLMService); // Dispose previous if any
            _agentState.LLMService = llmServiceResult.Data;
            _agentState.LLMService.MessageStreamed += OnLLMMessageStreamed;

            // --- Build Interrupting LLM (if needed) ---
            if (_agentState.CurrentConversationType == AgentConversationTypeENUM.InterruptibleViaAI)
            {
                // TODO: Adapt the logic from original code to build _interruptingLLMService
                // This needs careful handling of configuration (UseCurrentAgentLLMForInterrupting)
                // Ensure it also gets disposed on language change/shutdown
                _logger.LogWarning("Agent {AgentId}: Interrupting LLM setup logic needs to be fully implemented.", _agentState.AgentId);
                // Example placeholder:
                // var interruptingLLMResult = await BuildInterruptingLLMServiceAsync();
                // if (interruptingLLMResult.Success && interruptingLLMResult.Data != null)
                // {
                //     DisposeCurrentService(_agentState.InterruptingLLMService);
                //     _agentState.InterruptingLLMService = interruptingLLMResult.Data;
                //      SetupInterruptingLLMPrompt(); // Helper method
                // } else { throw... }
            }

            // --- Generate Base System Prompt ---
            await GenerateAndSetBaseSystemPromptAsync();

            // --- Warmup LLM ---
            await WarmupLLMAsync(); // Extracted logic

            _logger.LogInformation("LLM Handler initialized for Agent {AgentId}.", _agentState.AgentId);
            // --- End of moved logic ---
        }

        public async Task ReInitializeForLanguageAsync()
        {
            _logger.LogInformation("Agent {AgentId}: Re-initializing LLM Handler for new language.", _agentState.AgentId);
            // Cancel any ongoing task first
            await CancelCurrentLLMTaskAsync();
            await InitializeAsync(); // Re-runs full init logic
        }

        private async Task GenerateAndSetBaseSystemPromptAsync()
        {
            var systemPromptResult = await _systemPromptGenerator.GenerateInitialSystemPrompt(
               _agentState.BusinessApp!,
               _agentState.BusinessAppAgent!,
               _agentState.CurrentSessionRoute!,
               _agentState.CurrentLanguageCode,
               _agentState.LLMService!.GetProviderType(), // Assumes LLMService is initialized
               _agentState.LLMService!.GetModel()
            );
            if (!systemPromptResult.Success || systemPromptResult.Data == null)
            {
                _logger.LogError("Agent {AgentId}: Error generating system prompt: {Code} {Message}", _agentState.AgentId, systemPromptResult.Code, systemPromptResult.Message);
                // TODO: Raise error? Fallback to a default prompt?
                _agentState.LLMBaseSystemPrompt = "You are a helpful assistant."; // Basic fallback
            }
            else
            {
                _agentState.LLMBaseSystemPrompt = systemPromptResult.Data;
            }
            _agentState.LLMService!.SetSystemPrompt(_agentState.LLMBaseSystemPrompt); // Set base prompt initially
        }

        private async Task WarmupLLMAsync()
        {
            _logger.LogDebug("Warming up LLM for Agent {AgentId}.", _agentState.AgentId);
            // --- Move LLM warmup logic here ---
            _agentState.LLMService!.ClearMessages(); // Ensure clean state for warmup
            _agentState.LLMService!.AddUserMessage("response_from_system: Call has started.");
            _agentState.LLMService!.SetSystemPrompt("RESPOND WITH ```execute_system_function: acknowledge(\"Call Start\")``` if call has started.");
            await _agentState.LLMService!.ProcessInputAsync(CancellationToken.None); // Use own token?
            _agentState.LLMService!.AddAssistantMessage("execute_system_function: acknowledge(\"Call Start\")");
            // Reset to actual system prompt after warmup
            _agentState.LLMService!.SetSystemPrompt(_agentState.LLMBaseSystemPrompt);
            _logger.LogDebug("LLM Warmup complete for Agent {AgentId}.", _agentState.AgentId);
        }


        public async Task ProcessUserTextAsync(string text, string? clientId, CancellationToken externalToken)
        {
            // --- Move logic from original ProcessTextAsync (LLM part) here ---
            // - Check agent state (_agentState.IsResponding, IsExecutingTool, etc.)
            // - Handle interruptions (This might delegate to InterruptionManager)
            // - If ready, call SendLLMMessageAsync
            // ---
            _logger.LogDebug("Agent {AgentId} processing text: '{Text}'", _agentState.AgentId, text);

            // NOTE: Interruption handling is complex and likely involves the InterruptionManager module.
            // This handler might just receive a signal *after* an interruption is confirmed,
            // or the InterruptionManager might directly call SendLLMMessageAsync.
            // Assuming for now that this is called when ready to send to LLM.

            if (_agentState.IsResponding || _agentState.IsExecutingSystemTool || _agentState.IsExecutingCustomTool)
            {
                _logger.LogWarning("Agent {AgentId}: Received text while busy ({Responding}, {SysTool}, {CustTool}). Behavior depends on interruption logic.",
                   _agentState.AgentId, _agentState.IsResponding, _agentState.IsExecutingSystemTool, _agentState.IsExecutingCustomTool);
                // TODO: Delegate to Interruption Manager to decide fate of this text?
                return;
            }

            // Combine agent CTS and external token
            using var combinedCTS = CancellationTokenSource.CreateLinkedTokenSource(_currentLLMProcessingTaskCTS.Token, externalToken);
            await SendLLMMessageAsync(text, clientId, combinedCTS.Token);
        }

        public async Task ProcessSystemMessageAsync(string text, CancellationToken externalToken)
        {
            // Used for feeding back tool results etc.
            _logger.LogDebug("Agent {AgentId} processing system message: '{Text}'", _agentState.AgentId, text);

            if (_agentState.IsResponding || _agentState.IsExecutingSystemTool || _agentState.IsExecutingCustomTool)
            {
                _logger.LogWarning("Agent {AgentId}: Received system message while busy. Queuing might be needed.", _agentState.AgentId);
                // TODO: Implement queuing or handling if necessary. For now, proceed cautiously.
                await CancelCurrentLLMTaskAsync(); // Cancel previous task before starting new one based on system msg
            }

            // Combine agent CTS and external token
            using var combinedCTS = CancellationTokenSource.CreateLinkedTokenSource(_currentLLMProcessingTaskCTS.Token, externalToken);
            // Note: clientId is null for system messages typically
            await SendLLMMessageAsync(text, null, combinedCTS.Token, true); // Flag as system message
        }


        private async Task SendLLMMessageAsync(string text, string? clientId, CancellationToken cancellationToken, bool isSystemMessage = false)
        {
            // --- Move logic from original SendLLMMessage here ---
            // Update client context if needed
            // Add message to LLM history (User or potentially System)
            // Update system prompt with session info
            // Start _llmService.ProcessInputAsync and store task in _llmTask

            if (_agentState.LLMService == null)
            {
                _logger.LogError("Agent {AgentId}: LLM Service is not available.", _agentState.AgentId);
                // TODO: Raise error
                return;
            }

            _agentState.CurrentClientId = clientId; // Update current client context
                                                    // TODO: Update _agentState.ClientContextMap if needed

            // Update system prompt with latest session info just before sending
            await UpdateSystemPromptWithSessionInfoAsync();

            if (isSystemMessage)
            {
                // Decide how to add system messages (might need specific role or format)
                _agentState.LLMService.AddUserMessage($"response_from_system: {text}"); // Example format
            }
            else
            {
                _agentState.LLMService.AddUserMessage($"customer_query: {text}");
            }


            // Reset state flags before starting new task
            _agentState.IsResponding = false;
            _agentState.IsExecutingSystemTool = false;
            _agentState.IsExecutingCustomTool = false;
            _responseBuffer.Clear();
            _currentResponseBufferRead = 0;


            _logger.LogInformation("Agent {AgentId}: Sending message to LLM.", _agentState.AgentId);
            // TODO: Invoke Thinking event via Orchestrator?

            // Start the LLM processing task
            _llmTask = _agentState.LLMService.ProcessInputAsync(cancellationToken);
            await _llmTask; // Wait for completion OR handle asynchronously depending on design

            // Error handling for the task itself might be needed if not awaited here
            if (_llmTask.IsFaulted)
            {
                _logger.LogError(_llmTask.Exception, "Agent {AgentId}: LLM processing task failed.", _agentState.AgentId);
                // TODO: Raise error event
            }
            _logger.LogDebug("Agent {AgentId}: LLM ProcessInputAsync task completed (Status: {Status}).", _agentState.AgentId, _llmTask.Status);
            _llmTask = null; // Clear the task reference once completed/handled
        }

        private async Task UpdateSystemPromptWithSessionInfoAsync()
        {
            // Regenerate system prompt with current session details
            var sessionFilledPromptResult = await _systemPromptGenerator.FillSessionInformationInPrompt(
                _agentState.LLMBaseSystemPrompt,
                _agentState.CurrentClientId ?? "UnknownClient", // Provide a default if null
                _agentState.CurrentSessionRoute!,
                _agentState.BusinessAppAgent!,
                _agentState.CurrentLanguageCode);

            if (!sessionFilledPromptResult.Success)
            {
                _logger.LogWarning("Agent {AgentId}: Error filling session information in prompt: {Message}. Using base prompt.", _agentState.AgentId, sessionFilledPromptResult.Message);
                _agentState.LLMService!.SetSystemPrompt(_agentState.LLMBaseSystemPrompt); // Fallback
                                                                                          // TODO: Raise warning/error event?
            }
            else
            {
                _agentState.LLMService!.SetSystemPrompt(sessionFilledPromptResult.Data);
                //_logger.LogTrace("Agent {AgentId}: Updated system prompt with session info.", _agentState.AgentId);
            }
        }

        private async void OnLLMMessageStreamed(object? sender, object responseObj)
        {
            // --- Move logic from original OnLLMMessageStreamed here ---
            // - Use _llmResponseLock
            // - Extract chunk using LLMStreamingChunkDataExtractHelper
            // - Append to _responseBuffer
            // - Determine response type (response_to_customer, execute_system_function, execute_custom_function)
            // - Set state flags (_agentState.IsResponding, etc.)
            // - Call HandleLLMResponseProcessingAsync, HandleLLMSystemToolResponseCompletedAsync, or HandleLLMCustomToolResponseCompletedAsync

            // Check if cancellation requested before processing
            if (_currentLLMProcessingTaskCTS.IsCancellationRequested)
            {
                _logger.LogInformation("Agent {AgentId}: LLM stream processing cancelled.", _agentState.AgentId);
                return;
            }

            await _llmResponseLock.WaitAsync(); // Use internal CTS? Or none? Let's use none for now.

            try
            {
                if (_currentLLMProcessingTaskCTS.IsCancellationRequested)
                {
                    _logger.LogInformation("Agent {AgentId}: LLM stream processing cancelled after acquiring lock.", _agentState.AgentId);
                    return; // Check again after acquiring lock
                }

                FunctionReturnResult<(string? deltaText, bool isEndOfResponse)?> chunkExtractResult = LLMStreamingChunkDataExtractHelper.GetChunkData(responseObj, _agentState.LLMService!.GetProviderType());
                if (!chunkExtractResult.Success || !chunkExtractResult.Data.HasValue)
                {
                    _logger.LogError("Agent {AgentId}: Error extracting LLM chunk, {Reason}", _agentState.AgentId, chunkExtractResult.Message);
                    // TODO: Raise error? Stop processing this response?
                    return;
                }
                (string? deltaText, bool isEndOfResponse) = chunkExtractResult.Data.Value;

                if (!string.IsNullOrEmpty(deltaText))
                {
                    _responseBuffer.Append(deltaText);
                    //_logger.LogTrace("Agent {AgentId}: LLM Delta: '{Delta}'", _agentState.AgentId, deltaText);
                }

                // --- Determine response type ---
                if (!_agentState.IsResponding && !_agentState.IsExecutingSystemTool && !_agentState.IsExecutingCustomTool)
                {
                    var fullText = _responseBuffer.ToString();
                    if (fullText.StartsWith("response_to_customer:"))
                    {
                        _logger.LogDebug("Agent {AgentId}: LLM response identified as: Speak", _agentState.AgentId);
                        _agentState.IsResponding = true;
                        _currentResponseBufferRead = "response_to_customer:".Length; // Skip prefix
                        _agentState.CurrentResponseDurationSpeakingStarted = null; // Reset start time
                        _agentState.CurrentResponseDuration = TimeSpan.Zero;      // Reset duration
                    }
                    else if (fullText.StartsWith("execute_system_function:"))
                    {
                        _logger.LogDebug("Agent {AgentId}: LLM response identified as: System Tool", _agentState.AgentId);
                        _agentState.IsExecutingSystemTool = true;
                        _currentResponseBufferRead = "execute_system_function:".Length; // Skip prefix (will read full on complete)
                    }
                    else if (fullText.StartsWith("execute_custom_function:"))
                    {
                        _logger.LogDebug("Agent {AgentId}: LLM response identified as: Custom Tool", _agentState.AgentId);
                        _agentState.IsExecutingCustomTool = true;
                        _currentResponseBufferRead = "execute_custom_function:".Length; // Skip prefix (will read full on complete)
                    }
                    // If none match yet, keep buffering. Add a timeout/length check?
                }

                // --- Handle based on type ---
                if (_agentState.IsResponding)
                {
                    await HandleLLMResponseProcessingAsync(deltaText, isEndOfResponse); // Pass end flag
                }

                // --- Handle completion ---
                if (isEndOfResponse)
                {
                    _logger.LogDebug("Agent {AgentId}: LLM stream ended.", _agentState.AgentId);
                    var finalResponse = _responseBuffer.ToString();

                    if (_agentState.IsResponding)
                    {
                        await HandleLLMResponseCompletedAsync(finalResponse);
                    }
                    else if (_agentState.IsExecutingSystemTool)
                    {
                        // Pass the full content after the prefix
                        var toolContent = finalResponse.Substring("execute_system_function:".Length).Trim();
                        _agentState.LLMService!.AddAssistantMessage(finalResponse); // Add full tool command to history
                        SystemToolExecutionRequested?.Invoke(toolContent); // Notify Orchestrator/ToolExecutor
                        ResetLLMState();
                    }
                    else if (_agentState.IsExecutingCustomTool)
                    {
                        // Pass the full content after the prefix
                        var toolContent = finalResponse.Substring("execute_custom_function:".Length).Trim();
                        _agentState.LLMService!.AddAssistantMessage(finalResponse); // Add full tool command to history
                        CustomToolExecutionRequested?.Invoke(toolContent); // Notify Orchestrator/ToolExecutor
                        ResetLLMState();
                    }
                    else
                    {
                        _logger.LogError("Agent {AgentId}: LLM response ended but type unknown or invalid: {Response}", _agentState.AgentId, finalResponse);
                        // Handle invalid response - maybe ask LLM to clarify?
                        ResetLLMState(); // Reset anyway
                        _agentState.LLMService!.AddUserMessage("response_from_system: Invalid response type received. Please start with 'response_to_customer:', 'execute_system_function:', or 'execute_custom_function:'.");
                        // Potentially trigger another LLM call here if needed using ProcessSystemMessageAsync
                        // await ProcessSystemMessageAsync("Invalid response type received...", CancellationToken.None);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {AgentId}: Error processing LLM streaming response", _agentState.AgentId);
                // TODO: Raise error event
                ResetLLMState(); // Attempt to reset state on error
            }
            finally
            {
                _llmResponseLock.Release();
            }
        }

        private async Task HandleLLMResponseProcessingAsync(string? deltaText, bool isEndOfResponse)
        {
            // --- Move logic from original HandleLLMResponseProcessingAsync ---
            // Determine if enough text is buffered (_responseBuffer, _currentResponseBufferRead) to form a sentence/chunk.
            // If so, extract the chunk, invoke SynthesizeTextRequested event, update _currentResponseBufferRead.
            // Invoke TextChunkGenerated event.

            if (!_agentState.IsResponding || _responseBuffer.Length <= _currentResponseBufferRead)
            {
                return; // Nothing new to process for speaking
            }

            string unprocessedText = _responseBuffer.ToString().Substring(_currentResponseBufferRead);
            if (string.IsNullOrEmpty(unprocessedText)) return;

            // Simple chunking strategy: Split on sentences or if a large chunk accumulates, or on end of response.
            // More sophisticated NLP sentence boundary detection could be used.
            bool isCompleteSentence = unprocessedText.TrimEnd().EndsWith(".") || unprocessedText.TrimEnd().EndsWith("!") || unprocessedText.TrimEnd().EndsWith("?");
            bool isLargeChunk = unprocessedText.Length > 100; // Configurable threshold
            bool shouldProcessChunk = isEndOfResponse || (isCompleteSentence && unprocessedText.Length > 10) || isLargeChunk; // Min length for sentences


            if (shouldProcessChunk)
            {
                string textToSynthesize;
                int chunkSize;

                if (!isEndOfResponse && !isCompleteSentence && isLargeChunk)
                {
                    // Find the last sensible place to split (e.g., punctuation or whitespace)
                    int splitPoint = unprocessedText.LastIndexOfAny(new[] { '.', '!', '?', ',', ';', ' ' }, unprocessedText.Length - 2); // Look backwards from near the end
                    if (splitPoint > 0)
                    {
                        textToSynthesize = unprocessedText.Substring(0, splitPoint + 1).Trim();
                        chunkSize = splitPoint + 1;
                    }
                    else
                    {
                        // Cannot find good split, take the whole chunk (might be less natural)
                        textToSynthesize = unprocessedText.Trim();
                        chunkSize = unprocessedText.Length;
                    }
                }
                else
                {
                    // It's a complete sentence, the end of response, or a large chunk we didn't split
                    textToSynthesize = unprocessedText.Trim();
                    chunkSize = unprocessedText.Length; // Use the length of the original unprocessed part
                }


                if (!string.IsNullOrWhiteSpace(textToSynthesize))
                {
                    // Only synthesize non-empty text
                    _logger.LogDebug("Agent {AgentId}: Requesting synthesis for: \"{Text}\"", _agentState.AgentId, textToSynthesize.Length > 50 ? textToSynthesize.Substring(0, 50) + "..." : textToSynthesize);

                    if (_agentState.CurrentResponseDurationSpeakingStarted == null)
                    {
                        _agentState.CurrentResponseDurationSpeakingStarted = DateTime.UtcNow; // Mark start time on first chunk
                    }

                    if (SynthesizeTextRequested != null)
                    {
                        await SynthesizeTextRequested.Invoke(textToSynthesize); // Let AudioOutput handle TTS/duration
                    }
                    if (TextChunkGenerated != null)
                    {
                        TextChunkGenerated.Invoke(textToSynthesize); // Notify listener (e.g. UI)
                    }
                    _currentResponseBufferRead += chunkSize; // Update read position
                }
                else
                {
                    // If the extracted chunk is empty after trimming, just advance the read pointer
                    _currentResponseBufferRead += chunkSize;
                }
            }
        }

        private async Task HandleLLMResponseCompletedAsync(string finalResponse)
        {
            // --- Move logic from original HandleLLMResponseCompletedAsync ---
            // Handle any remaining text in the buffer.
            // Wait for TTS/speaking to finish (this coordination needs thought - maybe AudioOutput signals completion?).
            // Add assistant message to history.
            // Reset state.

            // 1. Process any remaining text
            if (_agentState.IsResponding && _currentResponseBufferRead < _responseBuffer.Length)
            {
                var remainingText = _responseBuffer.ToString().Substring(_currentResponseBufferRead).Trim();
                if (!string.IsNullOrWhiteSpace(remainingText))
                {
                    _logger.LogDebug("Agent {AgentId}: Processing final text chunk: \"{Text}\"", _agentState.AgentId, remainingText.Length > 50 ? remainingText.Substring(0, 50) + "..." : remainingText);
                    if (SynthesizeTextRequested != null)
                    {
                        await SynthesizeTextRequested.Invoke(remainingText);
                    }
                    if (TextChunkGenerated != null)
                    {
                        TextChunkGenerated.Invoke(remainingText);
                    }
                    _currentResponseBufferRead = _responseBuffer.Length; // Mark all as read
                }
            }

            // 2. Add the *clean* response to history (remove prefix)
            var assistantMessage = finalResponse.StartsWith("response_to_customer:")
               ? finalResponse.Substring("response_to_customer:".Length).Trim()
               : finalResponse; // Should not happen if IsResponding is true, but safeguard
            _agentState.LLMService!.AddAssistantMessage(assistantMessage);


            // 3. Signal completion (AudioOutput module will handle actual silence detection/wait)
            ResponseHandlingComplete?.Invoke();

            // 4. Reset state *after* signaling (or before depending on exact flow needed)
            ResetLLMState();
        }


        public async Task CancelCurrentLLMTaskAsync()
        {
            _logger.LogInformation("Agent {AgentId}: Attempting to cancel current LLM task.", _agentState.AgentId);
            if (!_currentLLMProcessingTaskCTS.IsCancellationRequested)
            {
                _currentLLMProcessingTaskCTS.Cancel();
                _currentLLMProcessingTaskCTS.Dispose(); // Dispose old one
            }
            _currentLLMProcessingTaskCTS = new CancellationTokenSource(); // Create new one for next task

            // Wait briefly for the task to acknowledge cancellation? Optional.
            if (_llmTask != null && !_llmTask.IsCompleted)
            {
                try
                {
                    await Task.WhenAny(_llmTask, Task.Delay(500)); // Wait max 500ms
                    _logger.LogDebug("Agent {AgentId}: LLM task status after cancellation attempt: {Status}", _agentState.AgentId, _llmTask.Status);
                }
                catch { /* Ignore potential task cancelled exceptions */ }
            }
            _llmTask = null;
            ResetLLMState(); // Also reset flags etc when cancelling
        }

        private void ResetLLMState()
        {
            _responseBuffer.Clear();
            _currentResponseBufferRead = 0;
            _agentState.IsResponding = false;
            _agentState.IsExecutingSystemTool = false;
            _agentState.IsExecutingCustomTool = false;
            // Don't reset _agentState.CurrentResponseDuration/SpeakingStarted here, AudioOutput manages that lifecycle
            _logger.LogDebug("Agent {AgentId}: LLM state flags reset.", _agentState.AgentId);
        }

        private void DisposeCurrentService(IDisposable? service)
        {
            if (service == null) return;

            if (service is ILLMService llmService)
            {
                try
                {
                    llmService.MessageStreamed -= OnLLMMessageStreamed;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Agent {AgentId}: Exception unsubscribing LLM MessageStreamed.", _agentState.AgentId);
                }
            }
            // Add similar unsubscribe for interrupting LLM if it has events

            try
            {
                service.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Agent {AgentId}: Exception disposing LLM service object.", _agentState.AgentId);
            }
        }

        public void Dispose()
        {
            CancelCurrentLLMTaskAsync().Wait(TimeSpan.FromSeconds(1)); // Wait briefly
            DisposeCurrentService(_agentState.LLMService);
            _agentState.LLMService = null;
            DisposeCurrentService(_agentState.InterruptingLLMService);
            _agentState.InterruptingLLMService = null;
            _currentLLMProcessingTaskCTS?.Dispose();
            _llmResponseLock?.Dispose();
            _logger.LogDebug("LLM Handler disposed for Agent {AgentId}.", _agentState.AgentId);
        }
    }
}