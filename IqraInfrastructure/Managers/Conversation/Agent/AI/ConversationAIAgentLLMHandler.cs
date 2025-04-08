using IqraCore.Interfaces.AI;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.LLM.Providers.Helpers;
using IqraInfrastructure.Managers.Business;
using Microsoft.Extensions.Logging;
using System.Text;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Helper.Agent;
using MongoDB.Driver.Linq;

namespace IqraInfrastructure.Managers.Conversation.Agent.AI
{
    public class ConversationAIAgentLLMHandler : IDisposable
    {
        // Events for Orchestrator
        public event Func<string, Task>? SynthesizeTextRequested;
        public event Action<string>? TextChunkGenerated;
        public event Action? ResponseHandlingComplete;
        public event Func<string, Task>? SystemToolExecutionRequested;
        public event Func<string, Task>? CustomToolExecutionRequested;

        private readonly ILogger<ConversationAIAgentLLMHandler> _logger;
        private readonly ConversationAIAgentState _agentState;
        private readonly LLMProviderManager _llmProviderManager;
        private readonly BusinessManager _businessManager;
        private readonly SystemPromptGenerator _systemPromptGenerator;

        private readonly SemaphoreSlim _llmResponseLock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _currentLLMProcessingTaskCTS = new();
        private Task? _llmTask;

        // Buffers managed here now
        public string? CurrentlyProcessingMessage = null;
        private readonly StringBuilder _responseBuffer = new StringBuilder();
        private int _currentResponseBufferRead = 0;

        public ConversationAIAgentLLMHandler(
            ILoggerFactory loggerFactory,
            ConversationAIAgentState agentState,
            LLMProviderManager llmProviderManager,
            BusinessManager businessManager,
            SystemPromptGenerator systemPromptGenerator
        )
        {
            _logger = loggerFactory.CreateLogger<ConversationAIAgentLLMHandler>();
            _agentState = agentState;
            _llmProviderManager = llmProviderManager;
            _businessManager = businessManager;
            _systemPromptGenerator = systemPromptGenerator;
        }

        public async Task InitializeAsync()
        {
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

            DisposeCurrentLLMService(_agentState.LLMService); // Dispose previous if any
            _agentState.LLMService = llmServiceResult.Data;           

            // Build Interrupting LLM
            if (_agentState.CurrentConversationType == AgentInterruptionTypeENUM.InterruptibleViaAI)
            {
                // TODO build a seperate function for interruption llm initalization
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

            // Generate Base System Prompt
            await GenerateAndSetBaseSystemPromptAsync();

            // Warmup LLM
            await WarmupLLMAsync();

            

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
               _agentState.LLMService!.GetProviderType(),
               _agentState.LLMService!.GetModel()
            );
            if (!systemPromptResult.Success || systemPromptResult.Data == null)
            {
                _logger.LogError("Agent {AgentId}: Error generating system prompt: {Code} {Message}", _agentState.AgentId, systemPromptResult.Code, systemPromptResult.Message);
                // TODO: Raise error? Fallback to a default prompt?
                _agentState.LLMBaseSystemPrompt = "ONLY RESPOND WITH: execute_system_function: \"Failed to generate base system prompt\", \"I am sorry, we are currently not able to handle your call due to an error occuring. Good bye!\""; // Basic fallback todo will fail to speak for other langauges
            }
            else
            {
                _agentState.LLMBaseSystemPrompt = systemPromptResult.Data;
            }
            _agentState.LLMService!.SetSystemPrompt(_agentState.LLMBaseSystemPrompt);
        }

        private async Task WarmupLLMAsync()
        {
            _logger.LogDebug("Warming up LLM for Agent {AgentId}.", _agentState.AgentId);

            // LLM warmup logic
            _agentState.LLMService!.ClearMessages();

            _agentState.LLMService!.AddUserMessage("response_from_system: Call has started.");
            _agentState.LLMService!.SetSystemPrompt("RESPOND WITH ```execute_system_function: acknowledge(\"Call Start\")``` if call has started.");

            bool hasFinishedWarmingUp = false;
            bool successWarmup = false;
            string warmupText = string.Empty;
            _agentState.LLMService!.MessageStreamed += (object? sender, object responseObj) =>
            {
                FunctionReturnResult<(string? deltaText, bool isEndOfResponse)?> chunkExtractResult = LLMStreamingChunkDataExtractHelper.GetChunkData(responseObj, _agentState.LLMService!.GetProviderType());
                if (!chunkExtractResult.Success || !chunkExtractResult.Data.HasValue)
                {
                    _logger.LogError("Agent {AgentId}: Error extracting LLM chunk, {Reason}", _agentState.AgentId, chunkExtractResult.Message);
                    successWarmup = false;
                    hasFinishedWarmingUp = true;              
                    return;
                }
                (string? deltaText, bool isEndOfResponse) = chunkExtractResult.Data.Value;

                if (!string.IsNullOrEmpty(deltaText))
                {
                    warmupText += deltaText;
                }

                if (isEndOfResponse)
                {
                    warmupText = warmupText.ToLower();
                    successWarmup = warmupText.Contains("execute_system_function: acknowledge(\"Call Start\")") || (warmupText.Contains("execute_system_function") && warmupText.Contains("acknowledge"));
                    hasFinishedWarmingUp = true;
                }
            };
            await _agentState.LLMService!.ProcessInputAsync(_agentState.MasterCancellationToken);
            while (!hasFinishedWarmingUp)
            {
                await Task.Delay(10);
            }
            if (!successWarmup)
            {
                _logger.LogError("Agent {AgentId}: LLM warmup failed.", _agentState.AgentId);
                // todo let orchestrator know
            }
            _agentState.LLMService!.ClearMessageStreamed();
            _agentState.LLMService!.MessageStreamed += OnLLMMessageStreamed;

            // Reset to actual system prompt after warmup
            _agentState.LLMService?.SetSystemPrompt(_agentState.LLMBaseSystemPrompt);

            _logger.LogDebug("LLM Warmup complete for Agent {AgentId}.", _agentState.AgentId);
        }


        public async Task ProcessUserTextAsync(string text, string? clientId, CancellationToken externalToken)
        {
            _logger.LogInformation("Agent {AgentId} processing text: '{Text}'", _agentState.AgentId, text);

            if (_agentState.IsResponding || _agentState.IsExecutingSystemTool || _agentState.IsExecutingCustomTool)
            {
                _logger.LogWarning("Agent {AgentId}: Received text while busy (responding:{Responding}, systool:{SysTool}, custtool:{CustTool}). Behavior depends on interruption logic.",
                   _agentState.AgentId, _agentState.IsResponding, _agentState.IsExecutingSystemTool, _agentState.IsExecutingCustomTool);
                // TODO: Delegate to Interruption Manager to decide fate of this text?
                return;
            }

            // Combine agent CTS and external token
            using var combinedCTS = CancellationTokenSource.CreateLinkedTokenSource(_currentLLMProcessingTaskCTS.Token, externalToken, _agentState.MasterCancellationToken);
            await SendLLMMessageAsync(text, clientId, combinedCTS.Token);
        }

        public async Task ProcessSystemMessageAsync(string text, CancellationToken externalToken)
        {
            _logger.LogDebug("Agent {AgentId} processing system message: '{Text}'", _agentState.AgentId, text);

            if (_agentState.IsExecutingSystemTool || _agentState.IsExecutingCustomTool)
            {
                await CancelCurrentLLMTaskAsync();
            }

            // Combine agent CTS and external token
            using var combinedCTS = CancellationTokenSource.CreateLinkedTokenSource(_currentLLMProcessingTaskCTS.Token, externalToken, _agentState.MasterCancellationToken);
            // Note: clientId is null for system messages typically
            await SendLLMMessageAsync(text, null, combinedCTS.Token, true); // Flag as system message
        }


        private async Task SendLLMMessageAsync(string text, string? clientId, CancellationToken cancellationToken, bool isSystemMessage = false)
        {
            if (_agentState.LLMService == null)
            {
                _logger.LogError("Agent {AgentId}: LLM Service is not available.", _agentState.AgentId);
                // TODO: Raise error
                return;
            }

            CurrentlyProcessingMessage = text;
            _agentState.CurrentClientId = clientId;

            // Update current client context
            // TODO: Update _agentState.ClientContextMap if needed

            await UpdateSystemPromptWithSessionInfoAsync();

            if (isSystemMessage)
            {
                _agentState.LLMService.AddUserMessage($"response_from_system: {text}");
            }
            else
            {
                _agentState.LLMService.AddUserMessage($"customer_query: {text}");
            }

            // Reset state flags before starting new task - tho this should already be done? todo check
            _agentState.IsResponding = false;
            _agentState.IsExecutingSystemTool = false;
            _agentState.IsExecutingCustomTool = false;
            _responseBuffer.Clear();
            _currentResponseBufferRead = 0;

            _logger.LogInformation("Agent {AgentId}: Sending message to LLM.", _agentState.AgentId);

            _llmTask = _agentState.LLMService.ProcessInputAsync(cancellationToken);
            await _llmTask;
        }

        private async Task UpdateSystemPromptWithSessionInfoAsync()
        {
            // Regenerate system prompt with current session details
            var sessionFilledPromptResult = await _systemPromptGenerator.FillSessionInformationInPrompt(
                _agentState.LLMBaseSystemPrompt,
                _agentState.CurrentClientId ?? "UnknownClient", // todo this is wrong, we need to make it primary for now, in future we will see how to handle it if multiple clients, maybe just let know in customer_query and adding all callers ids in the session information
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
            }
        }

        private async void OnLLMMessageStreamed(object? sender, object responseObj)
        {
            var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_currentLLMProcessingTaskCTS.Token, _agentState.MasterCancellationToken);

            // Check if cancellation requested before processing
            if (combinedCancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Agent {AgentId}: LLM stream processing cancelled.", _agentState.AgentId);
                return;
            }

            try
            {
                await _llmResponseLock.WaitAsync(combinedCancellationToken.Token);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation("Agent {AgentId}: LLM stream processing cancelled during lock waiting.", _agentState.AgentId);
                return;
            }

            try
            {
                if (combinedCancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Agent {AgentId}: LLM stream processing cancelled after acquiring lock.", _agentState.AgentId);
                    return;
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
                }

                // Determine response type first time
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
                        _currentResponseBufferRead = "execute_system_function:".Length;
                    }
                    else if (fullText.StartsWith("execute_custom_function:"))
                    {
                        _logger.LogDebug("Agent {AgentId}: LLM response identified as: Custom Tool", _agentState.AgentId);
                        _agentState.IsExecutingCustomTool = true;
                        _currentResponseBufferRead = "execute_custom_function:".Length;
                    }
                }

                // if currently in responding mode
                if (_agentState.IsResponding)
                {
                    await HandleLLMResponseProcessingAsync(deltaText, isEndOfResponse);
                }

                // if llm stream task is complete
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
                        var toolContent = finalResponse.Substring(_currentResponseBufferRead).Trim();
                        _agentState.LLMService!.AddAssistantMessage(finalResponse);
                        SystemToolExecutionRequested?.Invoke(toolContent);
                        _logger.LogInformation("Agent {AgentId}: System tool execution completed.", _agentState.AgentId);
                        CurrentlyProcessingMessage = null;
                    }
                    else if (_agentState.IsExecutingCustomTool)
                    {
                        // Pass the full content after the prefix
                        var toolContent = finalResponse.Substring(_currentResponseBufferRead).Trim();
                        _agentState.LLMService!.AddAssistantMessage(finalResponse);
                        CustomToolExecutionRequested?.Invoke(toolContent);
                        _logger.LogInformation("Agent {AgentId}: Custom tool execution completed.", _agentState.AgentId);
                        CurrentlyProcessingMessage = null;
                    }
                    else
                    {
                        _logger.LogError("Agent {AgentId}: LLM response ended but type unknown or invalid: {Response}", _agentState.AgentId, finalResponse);
                        
                        ResetLLMState(); // Reset anyway

                        await ProcessSystemMessageAsync("Invalid response type received. Please start with 'response_to_customer:', 'execute_system_function:', or 'execute_custom_function:'.", CancellationToken.None);
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                // expected
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {AgentId}: Error processing LLM streaming response", _agentState.AgentId);
                // TODO: Raise error event
                ResetLLMState(); // Attempt to reset state on error
            }
            finally
            {
                try
                {
                    _llmResponseLock.Release();
                }
                catch(ObjectDisposedException)
                {
                    // ignore, should not have been disposed tho check why TODO
                }
                catch
                {
                    throw;
                }
            }
        }

        private async Task HandleLLMResponseProcessingAsync(string? deltaText, bool isEndOfResponse)
        {
            if (!_agentState.IsResponding || _responseBuffer.Length <= _currentResponseBufferRead)
            {
                return; // Nothing new to process for speaking
            }

            string unprocessedText = _responseBuffer.ToString().Substring(_currentResponseBufferRead);
            if (string.IsNullOrEmpty(unprocessedText)) return;

            // Simple chunking strategy: Split on sentences or if a large chunk accumulates, or on end of response.
            // TODO More sophisticated NLP sentence boundary detection could be used.
            bool isCompleteSentence = unprocessedText.TrimEnd().EndsWith(".") || unprocessedText.TrimEnd().EndsWith("!") || unprocessedText.TrimEnd().EndsWith("?");
            bool isLargeChunk = unprocessedText.Length > 100; 
            bool shouldProcessChunk = isEndOfResponse || isCompleteSentence && unprocessedText.Length > 10 || isLargeChunk; // Min length for sentences

            if (shouldProcessChunk)
            {
                string textToSynthesize;
                int chunkSize;

                if (!isEndOfResponse && !isCompleteSentence && isLargeChunk)
                {
                    // Find the last sensible place to split (e.g., punctuation or whitespace)
                    int splitPoint = unprocessedText.LastIndexOfAny(new[] { '.', '!', '?', ',', ';' }, unprocessedText.Length - 2); // Look backwards from near the end
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
                    textToSynthesize = unprocessedText.Trim();
                    chunkSize = unprocessedText.Length;
                }


                if (!string.IsNullOrWhiteSpace(textToSynthesize))
                {
                    _logger.LogDebug("Agent {AgentId}: Requesting synthesis for: \"{Text}\"", _agentState.AgentId, textToSynthesize.Length > 50 ? textToSynthesize.Substring(0, 50) + "..." : textToSynthesize);

                    if (_agentState.CurrentResponseDurationSpeakingStarted == null)
                    {
                        _agentState.CurrentResponseDurationSpeakingStarted = DateTime.UtcNow;
                    }

                    if (SynthesizeTextRequested != null)
                    {
                        await SynthesizeTextRequested.Invoke(textToSynthesize);
                    }
                    if (TextChunkGenerated != null)
                    {
                        TextChunkGenerated.Invoke(textToSynthesize);
                    }
                    _currentResponseBufferRead += chunkSize;
                }
            }
        }

        private async Task HandleLLMResponseCompletedAsync(string finalResponse)
        {
            try
            {
                if (_agentState.IsResponding && _currentResponseBufferRead < _responseBuffer.Length)
                {
                    var remainingText = _responseBuffer.ToString().Substring(_currentResponseBufferRead).Trim();
                    if (!string.IsNullOrWhiteSpace(remainingText))
                    {
                        if (SynthesizeTextRequested != null)
                        {
                            await SynthesizeTextRequested.Invoke(remainingText);
                        }
                        if (TextChunkGenerated != null)
                        {
                            TextChunkGenerated.Invoke(remainingText);
                        }
                        _currentResponseBufferRead = _responseBuffer.Length;
                    }
                }

                var assistantMessage = _responseBuffer.ToString();
                _agentState.LLMService!.AddAssistantMessage(assistantMessage);

                using var combinedCTS = CancellationTokenSource.CreateLinkedTokenSource(_currentLLMProcessingTaskCTS.Token, _agentState.MasterCancellationToken);
                while (_agentState.AudioDurationLeftToPlay > TimeSpan.Zero)
                {
                    await Task.Delay(10, combinedCTS.Token);
                }

                CurrentlyProcessingMessage = null;
                ResponseHandlingComplete?.Invoke();
                ResetLLMState();
            }
            catch (OperationCanceledException ex)
            {
                // expected
            }
            catch (Exception ex)
            {
                throw;
            }
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

        public string GetCurrentResponseText()
        {
            return _responseBuffer.ToString();
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

        private void DisposeCurrentLLMService(IDisposable? service)
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
            // TODO make seperate function Add similar unsubscribe for interrupting LLM if it has events

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
            DisposeCurrentLLMService(_agentState.LLMService);
            _agentState.LLMService = null;
            DisposeCurrentLLMService(_agentState.InterruptingLLMService);
            _agentState.InterruptingLLMService = null;
            _currentLLMProcessingTaskCTS?.Dispose();
            _llmResponseLock?.Dispose();
            _logger.LogDebug("LLM Handler disposed for Agent {AgentId}.", _agentState.AgentId);
        }
    }
}