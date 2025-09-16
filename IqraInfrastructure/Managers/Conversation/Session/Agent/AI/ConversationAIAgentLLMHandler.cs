using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Conversation.Turn;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.AI;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Conversation.Session.Agent.AI.Helpers;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.LLM.Providers.Helpers;
using Microsoft.Extensions.Logging;
using System.Text;

namespace IqraInfrastructure.Managers.Conversation.Session.Agent.AI
{
    public class ConversationAIAgentLLMHandler : IDisposable
    {
        // Events for Orchestrator
        public event Func<ConversationTurn, string, Task>? SynthesizeTextSegmentRequested;

        public event Func<ConversationTurn, Task>? SystemToolExecutionRequested;
        public event Func<ConversationTurn, Task>? CustomToolExecutionRequested;

        private readonly ILogger<ConversationAIAgentLLMHandler> _logger;
        private readonly ConversationAIAgentState _agentState;
        private readonly LLMProviderManager _llmProviderManager;
        private readonly BusinessManager _businessManager;
        private readonly SystemPromptGenerator _systemPromptGenerator;

        private readonly SemaphoreSlim _llmResponseLock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _currentLLMProcessingTaskCTS = new();
        private Task? _llmTask;

        private readonly StringBuilder _responseBuffer = new StringBuilder();

        // Buffers managed here now
        public string? CurrentlyProcessingMessage = null;

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

        // Initalize
        public async Task InitializeAsync()
        {
            if (_agentState.BusinessAppAgent == null || string.IsNullOrEmpty(_agentState.CurrentLanguageCode) || _agentState.AgentConfiguration == null)
            {
                _logger.LogError("Agent {AgentId}: Cannot initialize LLM Handler - missing required state.", _agentState.AgentId);
                throw new InvalidOperationException("LLM Handler requires BusinessAppAgent, LanguageCode, and AgentConfiguration in state.");
            }

            // --- Build Primary LLM ---
            var defaultLLMServiceInfo = _agentState.BusinessAppAgent.Integrations.LLM[_agentState.CurrentLanguageCode][0];
            var llmBusinessIntegrationDataResult = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(_agentState.BusinessApp.Id, defaultLLMServiceInfo.Id);
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

            // Generate Base System Prompt
            await GenerateAndSetBaseSystemPromptAsync();

            // Warmup LLM
            await WarmupLLMAsync();
        }
        public async Task ReInitializeForLanguageAsync()
        {
            // Cancel any ongoing task first
            await CancelCurrentLLMTaskAsync();
            await InitializeAsync(); // Re-runs full init logic
        }

        // Warmup Initalization
        private const string PromptGenFailEndCallMessage = "ONLY RESPOND WITH: execute_system_function: end_call: \"Failed to generate base system prompt\", \"I am sorry, we are currently not able to handle your call due to an error occuring. Good bye!\"";
        private async Task GenerateAndSetBaseSystemPromptAsync()
        {
            var systemPromptResult = await _systemPromptGenerator.GenerateInitialSystemPrompt(
               _agentState.BusinessApp!,
               _agentState.BusinessAppAgent!,
               _agentState.CurrentSessionContext!,
               _agentState.CurrentLanguageCode,
               _agentState.LLMService!.GetProviderType(),
               _agentState.LLMService!.GetModel()
            );

            if (!systemPromptResult.Success || systemPromptResult.Data == null)
            {
                _logger.LogError("Agent {AgentId}: Error generating system prompt: {Code} {Message}", _agentState.AgentId, systemPromptResult.Code, systemPromptResult.Message);
                // TODO: Raise error? Fallback to a default prompt?
                _agentState.LLMService!.SetSystemPrompt(PromptGenFailEndCallMessage);
                return;
            }
            else
            {
                var sessionInformationResult = await _systemPromptGenerator.FillSessionInformationInPrompt(
                _agentState.LLMBaseSystemPrompt,
                _agentState.CurrentClientId ?? "UnknownClient", // todo this is wrong, we need to make it primary for now, in future we will see how to handle it if multiple clients, maybe just let know in customer_query and adding all callers ids in the session information
                _agentState.CurrentSessionContext!,
                _agentState.BusinessAppAgent!,
                _agentState.CurrentLanguageCode);
                if (!sessionInformationResult.Success || sessionInformationResult.Data == null)
                {
                    _logger.LogError("Agent {AgentId}: Error generating system prompt: {Code} {Message}", _agentState.AgentId, sessionInformationResult.Code, sessionInformationResult.Message);
                    // raise error add log todo
                    _agentState.LLMService!.SetSystemPrompt(PromptGenFailEndCallMessage);
                    return;
                }

                _agentState.LLMBaseSystemPrompt = systemPromptResult.Data + Environment.NewLine + Environment.NewLine + sessionInformationResult.Data;
                _agentState.LLMService!.SetSystemPrompt(_agentState.LLMBaseSystemPrompt);
            }        
        }
        private async Task WarmupLLMAsync()
        {
            // LLM warmup logic
            _agentState.LLMService!.ClearMessages();

            string openingMessage = "response_from_system: Call has started.";
            _agentState.LLMService!.AddUserMessage(openingMessage);
            TextRecievedForLLMToProcess?.Invoke(openingMessage, "system");
            _agentState.LLMService!.SetSystemPrompt("RESPOND WITH ```execute_system_function: acknowledge(\"Call Start\")``` if call has started.");

            bool hasFinishedWarmingUp = false;
            bool successWarmup = false;
            string warmupText = string.Empty;
            _agentState.LLMService!.MessageStreamed += (sender, eventobject) =>
            {
                FunctionReturnResult<(string? deltaText, bool isEndOfResponse)?> chunkExtractResult = LLMStreamingChunkDataExtractHelper.GetChunkData(eventobject.ResponseObject, _agentState.LLMService!.GetProviderType());
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
                    successWarmup = warmupText.Contains("execute_system_function: acknowledge(\"Call Start\")") || warmupText.Contains("execute_system_function") && warmupText.Contains("acknowledge");
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

        // Management
        public async Task ProcessUserTextAsync(ConversationTurn turn, CancellationToken externalToken)
        {
            using var combinedCTS = CancellationTokenSource.CreateLinkedTokenSource(_currentLLMProcessingTaskCTS.Token, externalToken, _agentState.MasterCancellationToken);
            await SendLLMMessageAsync(turn, combinedCTS.Token);
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
        
        // Message Processing
        private async Task SendLLMMessageAsync(ConversationTurn turn, CancellationToken cancellationToken)
        {
            if (_agentState.LLMService == null)
            {
                _logger.LogError("Agent {AgentId}: LLM Service is not available.", _agentState.AgentId);
                // TODO: Raise error
                return;
            }

            var currentDateTimeData = await _systemPromptGenerator.GenerateDateTimeInformationForMessage(null, _agentState.CurrentSessionContext.Agent.Timezones);
            if (!currentDateTimeData.Success || currentDateTimeData.Data == null)
            {
                _logger.LogError("Agent {AgentId}: Error generating date time information for message.", _agentState.AgentId);
            }

            string messageToSend = $"customer_query: {turn.User.TranscribedText}";
            _agentState.LLMService.AddUserMessage(messageToSend);

            var cacheableResult = await IsTextCacheable(turn.User.TranscribedText);
            if (cacheableResult.isCacheable && cacheableResult.cachedQuery != null)
            {
                OnLLMMessageStreamed(this, new ConversationAgentEventLLMStreamed($"response_to_customer: {cacheableResult.cachedQuery}", true));
                return; // No need to process further
            }

            _llmTask = _agentState.LLMService.ProcessInputAsync(cancellationToken, currentDateTimeData.Data, null);
         
            await _llmTask;
        }
        private async Task<(bool isCacheable, string? cachedQuery)> IsTextCacheable(string text)
        {
            var agent = _agentState.BusinessAppAgent;
            if (agent == null) return (false, null);

            var manuallyAssignedGroupIds = agent.Cache.Messages;
            if (manuallyAssignedGroupIds != null && manuallyAssignedGroupIds.Any())
            {
                var messageCacheGroups = _agentState.BusinessApp.Cache.MessageGroups
                    .Where(g => manuallyAssignedGroupIds.Contains(g.Id));

                foreach (var group in messageCacheGroups)
                {
                    if (group.Messages.TryGetValue(_agentState.CurrentLanguageCode, out var messagesList))
                    {
                        var cachedQuery = messagesList.FirstOrDefault(m => m.Query.Equals(text, StringComparison.OrdinalIgnoreCase));
                        if (cachedQuery != null)
                        {
                            _logger.LogTrace("Agent {AgentId}: Text '{Text}' is eligible for caching (manual group).", _agentState.AgentId, text);
                            return (true, cachedQuery.Response);
                        }
                    }
                }
            }

            return (false, null);
        }
        
        // Streamed Message (Event) Processing
        private async void OnLLMMessageStreamed(object? sender, ConversationAgentEventLLMStreamed eventData)
        {
            var currentTurn = _agentState.CurrentTurn;
            if (currentTurn == null)
            {
                _logger.LogError("LLM stream received but there is no active turn in the agent state.");
                return;
            }

            if (currentTurn.Response.StartedAt == null) // A good proxy for the start of a new response
            {
                _responseBuffer.Clear();
            }

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

                string? deltaText;
                bool isEndOfResponse;
                if (eventData.IsCachedResponse)
                {
                    deltaText = (string)eventData.ResponseObject;
                    isEndOfResponse = true; // Cached responses are always complete
                }
                else
                {
                    FunctionReturnResult<(string? deltaText, bool isEndOfResponse)?> chunkExtractResult = LLMStreamingChunkDataExtractHelper.GetChunkData(eventData.ResponseObject, _agentState.LLMService!.GetProviderType());
                    if (!chunkExtractResult.Success || !chunkExtractResult.Data.HasValue)
                    {
                        _logger.LogError("Agent {AgentId}: Error extracting LLM chunk, {Reason}", _agentState.AgentId, chunkExtractResult.Message);
                        // TODO: Raise error? Stop processing this response?
                        return;
                    }

                    deltaText = chunkExtractResult.Data.Value.deltaText;
                    isEndOfResponse = chunkExtractResult.Data.Value.isEndOfResponse;
                }

                if (!string.IsNullOrEmpty(deltaText))
                {
                    _responseBuffer.Append(deltaText);
                }

                // Determine response type first time
                if (currentTurn.Response.Type == AgentResponseType.NotSet)
                {
                    var fullText = _responseBuffer.ToString().TrimStart();
                    if (fullText.StartsWith("response_to_customer:"))
                    {
                        currentTurn.Response.Type = AgentResponseType.Speech;
                        currentTurn.Status = TurnStatus.AgentRespondingSpeech;    // Reset duration
                    }
                    else if (fullText.StartsWith("execute_system_function:"))
                    {
                        currentTurn.Response.Type = AgentResponseType.SystemTool;
                        currentTurn.Status = TurnStatus.AgentExecutingTool;
                        currentTurn.Response.ToolExecution = new ToolExecutionData { ToolType = AgentToolType.System };
                    }
                    else if (fullText.StartsWith("execute_custom_function:"))
                    {
                        currentTurn.Response.Type = AgentResponseType.CustomTool;
                        currentTurn.Status = TurnStatus.AgentExecutingTool;
                        currentTurn.Response.ToolExecution = new ToolExecutionData { ToolType = AgentToolType.Custom };
                    }

                    if (currentTurn.Response.Type != AgentResponseType.NotSet)
                    {
                        currentTurn.Response.StartedAt = DateTime.UtcNow;
                        await _conversationSession.NotifyTurnUpdated(currentTurn);
                    }
                }

                // Process based on type
                if (currentTurn.Response.Type == AgentResponseType.Speech)
                {
                    // The buffer now contains the complete text streamed so far.
                    // We compare it to what we've already sent for synthesis.
                    string prefix = "response_to_customer:";
                    string fullSpeechText = _responseBuffer.ToString().Substring(prefix.Length);
                    string alreadySynthesizedText = string.Join("", currentTurn.Response.SpokenSegments.Select(s => s.Text));

                    // Find the new, unprocessed text.
                    if (fullSpeechText.Length > alreadySynthesizedText.Length)
                    {
                        string newTextToProcess = fullSpeechText.Substring(alreadySynthesizedText.Length);

                        // Now, we check this *new* chunk for sentence boundaries.
                        if (ShouldProcessChunk(newTextToProcess, isEndOfResponse))
                        {
                            // Fire the event with ONLY the new, clean text segment.
                            // The AudioOutputHandler will add this to the turn's SpokenSegments list.
                            await SynthesizeTextSegmentRequested?.Invoke(currentTurn, newTextToProcess.Trim());
                        }
                    }
                }

                // if llm stream task is complete
                if (isEndOfResponse)
                {
                    if (currentTurn.Response.Type == AgentResponseType.SystemTool || currentTurn.Response.Type == AgentResponseType.CustomTool)
                    {
                        currentTurn.Response.ToolExecution.RawLLMInput = _responseBuffer.ToString();

                        if (currentTurn.Response.Type == AgentResponseType.SystemTool)
                        {
                            await SystemToolExecutionRequested?.Invoke(currentTurn);
                        }
                        else
                        {
                            await CustomToolExecutionRequested?.Invoke(currentTurn);
                        }
                    }
                    else if (currentTurn.Response.Type == AgentResponseType.Speech)
                    {
                        // The LLM part is done. The final text is in _responseBuffer.
                        // We don't need to do anything here except maybe one final DB update
                        // to ensure the full text is captured if there were unsynthesized fragments.
                        // The turn's final completion is now handled by the AudioOutput handler.
                        await _conversationSession.NotifyTurnUpdated(currentTurn);
                    }
                    else
                    {
                        _logger.LogError("Agent {AgentId}: LLM response ended but type unknown or invalid: {Response}", _agentState.AgentId, finalResponse);

                        _agentState.LLMService!.AddAssistantMessage(finalResponse);
                        AIAgentResponseCompleted?.Invoke(finalResponse);

                        ResetLLMState();

                        await ProcessSystemMessageAsync("Invalid response type received. Please start with 'response_to_customer:', 'execute_system_function:', or 'execute_custom_function:'.", _agentState.CurrentClientId, CancellationToken.None);
                    }

                    _responseBuffer.Clear();
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
            if (string.IsNullOrWhiteSpace(unprocessedText)) return;

            // Check for unxpected custom or system function calls
            int executionCustomFunctionStart = unprocessedText.IndexOf("execute_custom_function");
            if (executionCustomFunctionStart != -1)
            {
                if (executionCustomFunctionStart >= _currentResponseBufferRead)
                {
                    return;
                }
                else
                {
                    // delete everything after including the prefix
                    unprocessedText = unprocessedText.Substring(0, executionCustomFunctionStart);
                }
            }
            int executionSystemFunctionStart = unprocessedText.IndexOf("execute_system_function");
            if (executionSystemFunctionStart != -1)
            {
                if (executionSystemFunctionStart >= _currentResponseBufferRead)
                {
                    return;
                }
                else
                {
                    // delete everything after including the prefix
                    unprocessedText = unprocessedText.Substring(0, executionSystemFunctionStart);
                }
            }
            if (string.IsNullOrWhiteSpace(unprocessedText)) return;

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
                    _currentResponseBufferRead += chunkSize;
                }
            }
        }
        private async Task HandleLLMResponseCompletedAsync(string finalResponse)
        {
            try
            {
                var assistantMessage = _responseBuffer.ToString();
                // Check for unexpected custom or system function calls
                int executionCustomFunctionStart = assistantMessage.IndexOf("execute_custom_function");
                if (executionCustomFunctionStart != -1)
                {
                    _responseBuffer.Remove(executionCustomFunctionStart, _responseBuffer.Length - executionCustomFunctionStart);
                    assistantMessage = _responseBuffer.ToString();
                }
                int executionSystemFunctionStart = assistantMessage.IndexOf("execute_system_function");
                if (executionSystemFunctionStart != -1)
                {
                    _responseBuffer.Remove(executionSystemFunctionStart, _responseBuffer.Length - executionSystemFunctionStart);
                    assistantMessage = _responseBuffer.ToString();
                }

                if (_currentResponseBufferRead > _responseBuffer.Length) _currentResponseBufferRead = _responseBuffer.Length;

                if (_agentState.IsResponding && _currentResponseBufferRead < _responseBuffer.Length)
                {
                    var remainingText = _responseBuffer.ToString().Substring(_currentResponseBufferRead).Trim();
                    if (!string.IsNullOrWhiteSpace(remainingText))
                    {
                        if (SynthesizeTextRequested != null)
                        {
                            await SynthesizeTextRequested.Invoke(remainingText);
                        }
                        _currentResponseBufferRead = _responseBuffer.Length;
                    }
                }
               
                AIAgentResponseCompleted?.Invoke(assistantMessage);

                using var combinedCTS = CancellationTokenSource.CreateLinkedTokenSource(_currentLLMProcessingTaskCTS.Token, _agentState.MasterCancellationToken);
                while (_agentState.AudioDurationLeftToPlay > TimeSpan.Zero)
                {
                    await Task.Delay(10, combinedCTS.Token);
                }
       
                _agentState.LLMService!.AddAssistantMessage(assistantMessage);

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

        // Tool Result
        public async Task ProcessToolResultAsync(ConversationTurn turnWithToolResult, CancellationToken externalToken) // NEW
        {
            // A new entry point for when a tool finishes and we need to report back to the LLM.
            using var combinedCTS = CancellationTokenSource.CreateLinkedTokenSource(_currentLLMProcessingTaskCTS.Token, externalToken, _agentState.MasterCancellationToken);

            // The tool result is already in turnWithToolResult.Response.ToolExecution.Result
            // We just need to format it and send it.
            var messageToSend = $"response_from_system: {turnWithToolResult.Response.ToolExecution.Result}";
            _agentState.LLMService.AddUserMessage(messageToSend);

            await ProcessLlmInputForTurn(turnWithToolResult, combinedCTS.Token);
        }
        private async Task ProcessLlmInputForTurn(ConversationTurn turn, CancellationToken cancellationToken)
        {
            // This is a new helper to contain the core LLM call logic.
            _agentState.CurrentTurn = turn; // Ensure the state has the most up-to-date turn object.

            var currentDateTimeData = await _systemPromptGenerator.GenerateDateTimeInformationForMessage(...);

            _llmTask = _agentState.LLMService.ProcessInputAsync(cancellationToken, currentDateTimeData.Data, null);
            await _llmTask;
        }

        // Disposal
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
            _currentLLMProcessingTaskCTS?.Dispose();
            _llmResponseLock?.Dispose();
            _logger.LogDebug("LLM Handler disposed for Agent {AgentId}.", _agentState.AgentId);
        }
    }
}