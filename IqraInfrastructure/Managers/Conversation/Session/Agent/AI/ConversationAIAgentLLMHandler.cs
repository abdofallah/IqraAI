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
        private readonly ConversationSessionOrchestrator _conversationSession;

        private readonly SemaphoreSlim _llmResponseLock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _currentLLMProcessingTaskCTS = new();
        private Task? _llmTask;

        private readonly StringBuilder _responseBuffer = new StringBuilder();
        private int _currentResponseBufferReadPosition = 0;

        // Buffers managed here now
        public string? CurrentlyProcessingMessage = null;

        public ConversationAIAgentLLMHandler(
            ILoggerFactory loggerFactory,
            ConversationAIAgentState agentState,
            LLMProviderManager llmProviderManager,
            BusinessManager businessManager,
            SystemPromptGenerator systemPromptGenerator,
            ConversationSessionOrchestrator conversationSession
        )
        {
            _logger = loggerFactory.CreateLogger<ConversationAIAgentLLMHandler>();
            _agentState = agentState;
            _llmProviderManager = llmProviderManager;
            _businessManager = businessManager;
            _systemPromptGenerator = systemPromptGenerator;
            _conversationSession = conversationSession;
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
        public async Task ProcessUserTurnAsync(ConversationTurn turn, CancellationToken externalToken)
        {
            using var combinedCTS = CancellationTokenSource.CreateLinkedTokenSource(_currentLLMProcessingTaskCTS.Token, externalToken, _agentState.MasterCancellationToken);
            string messageToSend = $"customer_query: {turn.User.TranscribedText}";
            _agentState.LLMService!.AddUserMessage(messageToSend);
            await ProcessLLMInputForTurn(turn, combinedCTS.Token);
        }
        public async Task ProcessToolResultAsync(ConversationTurn turnWithToolResult, CancellationToken externalToken) // NEW
        {
            using var combinedCTS = CancellationTokenSource.CreateLinkedTokenSource(_currentLLMProcessingTaskCTS.Token, externalToken, _agentState.MasterCancellationToken);
            var messageToSend = $"response_from_system: {turnWithToolResult.Response.ToolExecution!.Result}";
            _agentState.LLMService!.AddUserMessage(messageToSend);
            await ProcessLLMInputForTurn(turnWithToolResult, combinedCTS.Token);
        }

        public async Task CancelCurrentLLMTaskAsync()
        {
            if (!_currentLLMProcessingTaskCTS.IsCancellationRequested)
            {
                _currentLLMProcessingTaskCTS.Cancel();
                _currentLLMProcessingTaskCTS.Dispose();
            }
            _currentLLMProcessingTaskCTS = new CancellationTokenSource();

            if (_llmTask != null && !_llmTask.IsCompleted)
            {
                try
                {
                    await Task.WhenAny(_llmTask, Task.Delay(500));
                }
                catch { /* Ignore potential task cancelled exceptions */ }
            }
            _llmTask = null;
            _responseBuffer.Clear();
            _currentResponseBufferReadPosition = 0;
        }

        // Message Processing
        private async Task ProcessLLMInputForTurn(ConversationTurn turn, CancellationToken cancellationToken)
        {
            _agentState.CurrentTurn = turn;

            var currentDateTimeData = await _systemPromptGenerator.GenerateDateTimeInformationForMessage(
                null, _agentState.CurrentSessionContext!.Agent.Timezones);

            var cacheableResult = await IsTextCacheable(turn.User.TranscribedText!);
            if (cacheableResult.isCacheable && cacheableResult.cachedQuery != null)
            {
                OnLLMMessageStreamed(this, new ConversationAgentEventLLMStreamed($"response_to_customer: {cacheableResult.cachedQuery}", true));
                return;
            }

            _llmTask = _agentState.LLMService!.ProcessInputAsync(cancellationToken, currentDateTimeData.Data, null);
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

            // On the first token of a new agent response, clear the buffer.
            if (currentTurn.Response.StartedAt == null)
            {
                _responseBuffer.Clear();
                _currentResponseBufferReadPosition = 0;
            }

            var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_currentLLMProcessingTaskCTS.Token, _agentState.MasterCancellationToken);
            if (combinedCancellationToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                await _llmResponseLock.WaitAsync(combinedCancellationToken.Token);
            }
            catch (OperationCanceledException ex)
            {
                return;
            }

            try
            {
                if (combinedCancellationToken.IsCancellationRequested)
                {
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
                        currentTurn.Status = TurnStatus.AgentRespondingSpeech;
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
                    if (_responseBuffer.Length > _currentResponseBufferReadPosition)
                    {
                        string prefix = "response_to_customer:";
                        if (_responseBuffer.Length > prefix.Length)
                        {
                            string unprocessedText = _responseBuffer.ToString().Substring(_currentResponseBufferReadPosition);

                            // Simple chunking strategy based on your previous robust implementation
                            bool isCompleteSentence = unprocessedText.TrimEnd().EndsWith('.') || unprocessedText.TrimEnd().EndsWith('!') || unprocessedText.TrimEnd().EndsWith('?');
                            bool isLargeChunk = unprocessedText.Length > 80;
                            bool shouldProcessChunk = isEndOfResponse || (isCompleteSentence && unprocessedText.Length > 10) || isLargeChunk;

                            if (shouldProcessChunk)
                            {
                                string textToSynthesize;
                                int chunkSize;

                                // On the final chunk, just send everything that's left.
                                if (isEndOfResponse)
                                {
                                    textToSynthesize = unprocessedText.Trim();
                                    chunkSize = unprocessedText.Length;
                                }
                                else
                                {
                                    // TODO handle the split point better with NLP boundaries
                                    // current issues, too small text
                                    // spilitting currencies/amounts e.g 2.1 million becomes two <pause> 1 million
                                    // Find a sensible split point
                                    int splitPoint = unprocessedText.LastIndexOfAny(new[] { '.', '!', '?', ',', ';', ':' }, unprocessedText.Length - 2);
                                    if (splitPoint > 0)
                                    {
                                        textToSynthesize = unprocessedText.Substring(0, splitPoint + 1).Trim();
                                        chunkSize = splitPoint + 1;
                                    }
                                    else // If no good split point, just send the whole large chunk
                                    {
                                        textToSynthesize = unprocessedText.Trim();
                                        chunkSize = unprocessedText.Length;
                                    }
                                }

                                if (!string.IsNullOrWhiteSpace(textToSynthesize))
                                {
                                    await SynthesizeTextSegmentRequested?.Invoke(currentTurn, textToSynthesize);
                                    _currentResponseBufferReadPosition += chunkSize;
                                }
                            }
                        }
                    }
                }

                // if llm stream task is complete
                if (isEndOfResponse)
                {
                    if (currentTurn.Response.Type == AgentResponseType.SystemTool || currentTurn.Response.Type == AgentResponseType.CustomTool)
                    {
                        currentTurn.Response.ToolExecution!.RawLLMInput = _responseBuffer.ToString();

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
                        await _conversationSession.NotifyTurnUpdated(currentTurn);
                    }
                    else
                    {
                        _logger.LogError("Agent {AgentId}: LLM response ended but type unknown or invalid: {Response}", _agentState.AgentId, _responseBuffer.ToString());

                        //_agentState.LLMService!.AddAssistantMessage(finalResponse);
                        //AIAgentResponseCompleted?.Invoke(finalResponse);

                        //await ProcessSystemMessageAsync("Invalid response type received. Please start with 'response_to_customer:', 'execute_system_function:', or 'execute_custom_function:'.", _agentState.CurrentClientId, CancellationToken.None);
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
        private bool ShouldProcessChunk(string newText, bool isEndOfResponse)
        {
            if (string.IsNullOrWhiteSpace(newText)) return false;

            bool isCompleteSentence = newText.TrimEnd().EndsWith(".") || newText.TrimEnd().EndsWith("!") || newText.TrimEnd().EndsWith("?");
            bool isLargeChunk = newText.Length > 80;
            return isEndOfResponse || (isCompleteSentence && newText.Length > 10) || isLargeChunk;
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