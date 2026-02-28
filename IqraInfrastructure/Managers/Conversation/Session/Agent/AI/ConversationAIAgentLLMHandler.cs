using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Conversation.Turn;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.AI;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Conversation.Session.Agent.AI.Helpers;
using IqraInfrastructure.Managers.Conversation.Session.Logger;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.LLM.Providers.Helpers;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace IqraInfrastructure.Managers.Conversation.Session.Agent.AI
{
    public class ConversationAIAgentLLMHandler : IDisposable
    {
        // Events for Orchestrator
        public event Func<ConversationTurn, string, bool, Task>? SynthesizeTextSegmentRequested;
        public event Func<ConversationTurn, Task>? SystemToolExecutionRequested;
        public event Func<ConversationTurn, Task>? CustomToolExecutionRequested;
        public event Func<Task>? LLMFailureAndEndCallRequested;

        private readonly SessionLoggerFactory _sessionLoggerFactory;
        private readonly ILogger<ConversationAIAgentLLMHandler> _logger;
        private readonly ConversationAIAgentState _agentState;
        private readonly LLMProviderManager _llmProviderManager;
        private readonly BusinessManager _businessManager;
        private readonly SystemPromptGenerator _systemPromptGenerator;
        private readonly ConversationSessionOrchestrator _conversationSession;
        private readonly ConversationAIAgentRAGManager _conversationAIAgentRAGManager;

        private readonly SemaphoreSlim _llmResponseLock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _currentLLMProcessingTaskCTS = new();

        private Task? _llmTask;
        private CancellationToken? _llmTaskCurrentCancelToken;
        private string? _llmTaskCurrentBeforeContextMessage;
        private ConversationTurn? _llmTaskCurrentTurn;

        private readonly StringBuilder _responseBuffer = new StringBuilder();
        private int _currentResponseBufferReadPosition = 0;

        // Buffers managed here now
        public string? CurrentlyProcessingMessage = null;

        private int CurrentLLMStreamFailedCount = 0;
        private int CurrentLLMInvalidResponseCount = 0;

        public ConversationAIAgentLLMHandler(
            SessionLoggerFactory loggerFactory,
            ConversationAIAgentState agentState,
            LLMProviderManager llmProviderManager,
            BusinessManager businessManager,
            SystemPromptGenerator systemPromptGenerator,
            ConversationSessionOrchestrator conversationSession,
            ConversationAIAgentRAGManager conversationAIAgentRAGManager
        )
        {
            _sessionLoggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<ConversationAIAgentLLMHandler>();
            _agentState = agentState;
            _llmProviderManager = llmProviderManager;
            _businessManager = businessManager;
            _systemPromptGenerator = systemPromptGenerator;
            _conversationSession = conversationSession;
            _conversationAIAgentRAGManager = conversationAIAgentRAGManager;
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

            var llmServiceResult = await _llmProviderManager.BuildProviderServiceByIntegration(_sessionLoggerFactory, _agentState.LLMBusinessIntegrationData, defaultLLMServiceInfo, new Dictionary<string, string> { });
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
        private async Task GenerateAndSetBaseSystemPromptAsync()
        {
            var systemPromptResult = await _systemPromptGenerator.GenerateInitialSystemPrompt(
               _agentState.BusinessApp!,
               _agentState.BusinessAppAgent!,
               _agentState.CurrentSessionContext!,
               _agentState.CurrentLanguageData!,
               _agentState.LLMService!.GetProviderType(),
               _agentState.LLMService!.GetModel()
            );

            if (!systemPromptResult.Success || systemPromptResult.Data == null)
            {
                _logger.LogError("Agent {AgentId}: Error generating system prompt: {Code} {Message}", _agentState.AgentId, systemPromptResult.Code, systemPromptResult.Message);
                // Fallback to a default prompt forcing end call
                _agentState.LLMService!.SetSystemPrompt(_agentState.CurrentLanguageData!.Prompts.FailedConversationBasePromptGenerationPrompt);
                return;
            }


            var sessionInformationResult = await _systemPromptGenerator.FillSessionInformationInPrompt(
                null, // TODO get from current language prompts
                _agentState.CurrentSessionContext!,
                _agentState.BusinessAppAgent!,
                _agentState.CurrentLanguageCode);
            if (!sessionInformationResult.Success || sessionInformationResult.Data == null)
            {
                _logger.LogError("Agent {AgentId}: Error generating system prompt: {Code} {Message}", _agentState.AgentId, sessionInformationResult.Code, sessionInformationResult.Message);
                // Fallback to a default prompt forcing end call
                _agentState.LLMService!.SetSystemPrompt(_agentState.CurrentLanguageData!.Prompts.FailedConversationBasePromptGenerationPrompt);
                return;
            }

            _agentState.LLMBaseSystemPrompt = systemPromptResult.Data + Environment.NewLine + Environment.NewLine + sessionInformationResult.Data;
            _agentState.LLMService!.SetSystemPrompt(_agentState.LLMBaseSystemPrompt);
            _logger.LogDebug("Agent {AgentId}: Generated base system prompt: {Prompt}", _agentState.AgentId, _agentState.LLMBaseSystemPrompt);
        }
        private async Task WarmupLLMAsync()
        {
            // LLM warmup logic
            _agentState.LLMService!.ClearMessages();

            string openingMessage = "response_from_system: Call has started.";
            _agentState.LLMService!.AddUserMessage(openingMessage);
            _agentState.LLMService!.SetSystemPrompt(_agentState.CurrentLanguageData!.Prompts.ConversationWarmupLLMPrompt);

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
            _agentState.LLMService!.MessageStreamedCancelled += OnLLMMessageStreamedCancelled;

            // Reset to actual system prompt after warmup
            _agentState.LLMService?.SetSystemPrompt(_agentState.LLMBaseSystemPrompt);

            _logger.LogDebug("LLM Warmup complete for Agent {AgentId}.", _agentState.AgentId);
        }

        // Management
        public async Task ProcessUserTurnAsync(ConversationTurn turn, CancellationToken externalToken)
        {
            using var combinedCTS = CancellationTokenSource.CreateLinkedTokenSource(_currentLLMProcessingTaskCTS.Token, externalToken, _agentState.MasterCancellationToken);
            string messageToSend = $"user_query: {turn.UserInput!.TranscribedText}";

            _logger.LogDebug("ProcessUserTurnAsync: Processing turn {turnId} for Agent {AgentId} with messageToSend: {messageToSend}.", turn.Id, _agentState.AgentId, ((messageToSend.Length > 100) ? messageToSend.Substring(0, 100) : messageToSend));

            _agentState.LLMService!.AddUserMessage(messageToSend);
            await ProcessLLMInputForTurn(turn, false, combinedCTS.Token);
        }
        public async Task ProcessToolResultAsync(ConversationTurn newToolResultTurn, ConversationTurn turnWithToolResult, CancellationToken externalToken) // NEW
        {
            using var combinedCTS = CancellationTokenSource.CreateLinkedTokenSource(_currentLLMProcessingTaskCTS.Token, externalToken, _agentState.MasterCancellationToken);
            var messageToSend = $"response_from_system: {turnWithToolResult.Response.ToolExecution!.Result}";

            _logger.LogDebug("ProcessToolResultAsync: Processing turn tool result for {turnWithToolResultId} for turn {newToolResultTurn} for Agent {AgentId} with messageToSend: {messageToSend}.", turnWithToolResult.Id, newToolResultTurn.Id, _agentState.AgentId, ((messageToSend.Length > 100) ? messageToSend.Substring(0, 100) : messageToSend));

            _agentState.LLMService!.AddUserMessage(messageToSend);
            await ProcessLLMInputForTurn(newToolResultTurn, true, combinedCTS.Token);
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
            _llmTaskCurrentCancelToken = null;
            _llmTaskCurrentBeforeContextMessage = null;
            _llmTaskCurrentTurn = null;
            _responseBuffer.Clear();
            _currentResponseBufferReadPosition = 0;
        }

        // Message Processing
        private async Task ProcessLLMInputForTurn(ConversationTurn turn, bool isToolResult, CancellationToken cancellationToken)
        {
            CurrentLLMInvalidResponseCount = 0;
            string beforeMessageContext = string.Empty;

            if (!isToolResult)
            {
                bool performKnowledgeBaseRetrieval = await _conversationAIAgentRAGManager.ShouldPerformSearchAsync(turn.UserInput.TranscribedText!, cancellationToken);
                if (performKnowledgeBaseRetrieval)
                {
                    Stopwatch ragRetrievalLatencyStopwatch = Stopwatch.StartNew();
                    var knowledgeBaseRetrivalResult = await _conversationAIAgentRAGManager.RetrieveResultsForQueryAsync(turn.UserInput.TranscribedText!, cancellationToken);
                    ragRetrievalLatencyStopwatch.Stop();

                    turn.Response.KnowledgeBaseRetrievalData = new ConversationTurnKnowledgeBaseRetrievalData()
                    {
                        WasSuccessfull = knowledgeBaseRetrivalResult.Success,
                        RetrievalLatencyMS = (int)ragRetrievalLatencyStopwatch.ElapsedMilliseconds
                    };

                    if (knowledgeBaseRetrivalResult.Success)
                    {
                        if (knowledgeBaseRetrivalResult.Data == null)
                        {
                            turn.Response.KnowledgeBaseRetrievalData.ResultMessage = knowledgeBaseRetrivalResult.Message;

                            beforeMessageContext = $"<KnowledgeBaseQueryRetrival>\n{knowledgeBaseRetrivalResult.Message}\n</KnowledgeBaseQueryRetrieval>";
                        }
                        else
                        {
                            turn.Response.KnowledgeBaseRetrievalData.RetrievedResultsMetaData = knowledgeBaseRetrivalResult.Data.Select(x => x.Metadata).ToList();

                            string contextConverted = _conversationAIAgentRAGManager.FormatResultsForContext(knowledgeBaseRetrivalResult.Data);
                            beforeMessageContext = $"<KnowledgeBaseQueryRetrival>\n{contextConverted}\n</KnowledgeBaseQueryRetrieval>";
                        }
                    }
                    else
                    {
                        turn.Response.KnowledgeBaseRetrievalData.ResultMessage = $"[{knowledgeBaseRetrivalResult.Code}] {knowledgeBaseRetrivalResult.Message}";
                    }

                    await _conversationSession.NotifyTurnUpdated(turn);
                }
            }

            // Since knowledge base retrieval takes time, turn could be cancelled in between so we need to check
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("ProcessLLMInputForTurn: turn {turnId} for Agent {AgentId} was cancelled before llm process started.", turn.Id, _agentState.AgentId);
                return;
            }

            var currentDateTimeData = await _systemPromptGenerator.GenerateDateTimeInformationForMessage(
                null, _agentState.CurrentSessionContext!.Agent.Timezones);
            if (currentDateTimeData.Success && !string.IsNullOrWhiteSpace(currentDateTimeData.Data))
            {
                beforeMessageContext += "\n" + currentDateTimeData.Data;
            }

            if (!isToolResult)
            {
                var cacheableResult = await IsTextCacheable(turn.UserInput.TranscribedText!);
                if (cacheableResult.isCacheable && cacheableResult.cachedQuery != null)
                {
                    _logger.LogDebug("Agent {AgentId}: Text '{Text}' is eligible for caching and using cached result '{CachedQuery}'.", _agentState.AgentId, (turn.UserInput.TranscribedText!.Length > 100) ? turn.UserInput.TranscribedText!.Substring(0, 100) + "..." : turn.UserInput.TranscribedText!, (cacheableResult.cachedQuery.Length > 100) ? cacheableResult.cachedQuery.Substring(0, 100) + "..." : cacheableResult.cachedQuery);

                    OnLLMMessageStreamed(this, new ConversationAgentEventLLMStreamed($"response_to_user: {cacheableResult.cachedQuery}", true));
                    return;
                }
            }

            turn.Response.LLMProcessStartedAt = DateTime.UtcNow;
            await _conversationSession.NotifyTurnUpdated(turn);

            _llmTask = _agentState.LLMService!.ProcessInputAsync(cancellationToken, beforeMessageContext, null);
            _llmTaskCurrentCancelToken = cancellationToken;
            _llmTaskCurrentBeforeContextMessage = beforeMessageContext;
            _llmTaskCurrentTurn = turn;
            _logger.LogDebug("Agent {AgentId}: LLM process started for turn {TurnId}", _agentState.AgentId, turn.Id);
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
        private async void OnLLMMessageStreamedCancelled(object? sender, ConversationAgentEventLLMStreamCancelled? eventData)
        {
            if (eventData == null) return;
            if (_llmTaskCurrentCancelToken == null || _llmTaskCurrentCancelToken.Value.IsCancellationRequested || _llmTaskCurrentTurn == null) return;

            if (eventData.Type == ConversationAgentEventLLMStreamCancelledTypeEnum.HttpRequestNotSuccess)
            {
                _logger.LogError("Agent {AgentId}: LLM stream cancelled due to http request not success.", _agentState.AgentId);

                CurrentLLMStreamFailedCount++;
                if (CurrentLLMStreamFailedCount > 3)
                {
                    _logger.LogError("Agent {AgentId}: LLM stream cancelled due to http request not success 3 times.", _agentState.AgentId);
                    //TODO force end the call for now, as fall back llm system is not yet implemented.
                    await LLMFailureAndEndCallRequested.Invoke();
                    return;
                }

                _llmTask = _agentState.LLMService!.ProcessInputAsync(_llmTaskCurrentCancelToken!.Value, _llmTaskCurrentBeforeContextMessage, null);
                _logger.LogDebug("Agent {AgentId}: LLM process started for turn {TurnId}", _agentState.AgentId, _llmTaskCurrentTurn!.Id);
                await _llmTask;
            }
            else
            {
                _logger.LogError("Agent {AgentId}: LLM stream cancelled due to unknown reason {Reason}.", _agentState.AgentId, eventData.Type);
            }
        }
        private async void OnLLMMessageStreamed(object? sender, ConversationAgentEventLLMStreamed eventData)
        {
            CurrentLLMStreamFailedCount = 0;

            var currentTurn = _agentState.CurrentTurn;
            if (currentTurn == null)
            {
                _logger.LogError(" Agent {AgentId}: LLM stream received but there is no active turn in the agent state.", _agentState.AgentId);
                return;
            }

            // On the first token of a new agent response, clear the buffer.
            if (currentTurn.Response.LLMStreamingStartedAt == null)
            {
                _logger.LogDebug("Agent {AgentId}: LLM stream started for turn {TurnId}, resetting response buffer.", _agentState.AgentId, currentTurn.Id);

                _responseBuffer.Clear();
                _currentResponseBufferReadPosition = 0;

                currentTurn.Response.LLMStreamingStartedAt = DateTime.UtcNow;
                currentTurn.Response.LLMProcessLatencyFirstTokenMS = (int)(currentTurn.Response.LLMStreamingStartedAt.Value - currentTurn.Response.LLMProcessStartedAt!.Value).TotalMilliseconds;
                await _conversationSession.NotifyTurnUpdated(currentTurn);
            }

            var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_currentLLMProcessingTaskCTS.Token, _agentState.MasterCancellationToken);
            if (combinedCancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Agent {AgentId}: LLM stream processing cancelled for turn {TurnId}.", _agentState.AgentId, currentTurn.Id);
                return;
            }

            try
            {
                await _llmResponseLock.WaitAsync(combinedCancellationToken.Token);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex, "Agent {AgentId}: LLM stream processing cancelled during response lock wait for turn {TurnId}.", _agentState.AgentId, currentTurn.Id);
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
                        _logger.LogError("Agent {AgentId}: Error extracting LLM chunk for turn {TurnId}, {Reason}", _agentState.AgentId, currentTurn.Id, chunkExtractResult.Message);
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
                if (currentTurn.Response.Type == ConversationTurnAgentResponseType.NotSet)
                {
                    var fullText = _responseBuffer.ToString().TrimStart();
                    if (fullText.StartsWith("response_to_user:"))
                    {
                        currentTurn.Response.Type = ConversationTurnAgentResponseType.Speech;
                        currentTurn.Status = ConversationTurnStatus.AgentRespondingSpeech;
                        await _conversationSession.NotifyTurnUpdated(currentTurn);

                        _logger.LogDebug("Agent {AgentId}: LLM response turn {TurnId} is a speech response.", _agentState.AgentId, currentTurn.Id);
                    }
                    else if (fullText.StartsWith("execute_system_function:"))
                    {
                        currentTurn.Response.Type = ConversationTurnAgentResponseType.SystemTool;
                        currentTurn.Status = ConversationTurnStatus.AgentProcessing;
                        currentTurn.Response.ToolExecution = new ConversationTurnToolExecutionData { ToolType = ConversationTurnAgentToolType.System };
                        await _conversationSession.NotifyTurnUpdated(currentTurn);

                        _logger.LogDebug("Agent {AgentId}: LLM response turn {TurnId} is a system tool response.", _agentState.AgentId, currentTurn.Id);
                    }
                    else if (fullText.StartsWith("execute_custom_function:"))
                    {
                        currentTurn.Response.Type = ConversationTurnAgentResponseType.CustomTool;
                        currentTurn.Status = ConversationTurnStatus.AgentProcessing;
                        currentTurn.Response.ToolExecution = new ConversationTurnToolExecutionData { ToolType = ConversationTurnAgentToolType.Custom };
                        await _conversationSession.NotifyTurnUpdated(currentTurn);

                        _logger.LogDebug("Agent {AgentId}: LLM response turn {TurnId} is a custom tool response.", _agentState.AgentId, currentTurn.Id);
                    }

                    if ((fullText.Length >= 30 || isEndOfResponse) && currentTurn.Response.Type == ConversationTurnAgentResponseType.NotSet)
                    {
                        CurrentLLMInvalidResponseCount++;
                        _logger.LogDebug("Agent {AgentId}: LLM response turn {TurnId} has a long first token which is unexpected so cancelling and recalling.", _agentState.AgentId, currentTurn.Id);

                        if (CurrentLLMInvalidResponseCount > 3)
                        {
                            _logger.LogError("Agent {AgentId}: LLM response turn {TurnId} has too many invalid responses, cancelling.", _agentState.AgentId, currentTurn.Id);

                            await LLMFailureAndEndCallRequested.Invoke();
                            return;
                        }

                        var currentCancelToken = _llmTaskCurrentCancelToken;
                        var currentBeforeContextMessage = _llmTaskCurrentBeforeContextMessage;
                        var currentFailTurn = _llmTaskCurrentTurn;

                        await CancelCurrentLLMTaskAsync();

                        _agentState.LLMService!.AddAssistantMessage($"{fullText}...");
                        _agentState.LLMService.AddUserMessage($"response_from_system: Invalid response type received. Please start with 'response_to_user:', 'execute_system_function:', 'execute_custom_function:' or 'execute_flowapp_action:'.");

                        _logger.LogDebug("Agent {AgentId}: LLM response turn {TurnId} is an invalid type so retrying.", _agentState.AgentId, currentTurn.Id);
                        _llmTask = _agentState.LLMService!.ProcessInputAsync(currentCancelToken!.Value, currentBeforeContextMessage, null);
                        return;
                    }
                }

                // Process based on type
                if (currentTurn.Response.Type == ConversationTurnAgentResponseType.Speech)
                {
                    if (_responseBuffer.Length > _currentResponseBufferReadPosition)
                    {
                        string prefix = "response_to_user:";
                        if (_responseBuffer.Length > prefix.Length)
                        {
                            string unprocessedText = _responseBuffer.ToString().Substring(_currentResponseBufferReadPosition + prefix.Length);

                            // Simple chunking strategy based on your previous robust implementation
                            // todo have a better type of chunking based on if its a new turn, speak less, then start chunking longer based on how much speech is in the buffer.
                            bool isCompleteSentence = unprocessedText.TrimEnd().EndsWith('.') || unprocessedText.TrimEnd().EndsWith('!') || unprocessedText.TrimEnd().EndsWith('?');
                            bool isLargeChunk = unprocessedText.Length > 80; // todo this can be problematic, we need to make this dynamic or better
                            bool shouldProcessChunk = isEndOfResponse || (isCompleteSentence && unprocessedText.Length > 30) || isLargeChunk;

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

                                _logger.LogDebug("Agent {AgentId}: Synthesizing text chunk of length {ChunkSize} for turn {TurnId}.", _agentState.AgentId, chunkSize, currentTurn.Id);

                                await SynthesizeTextSegmentRequested?.Invoke(currentTurn, textToSynthesize, isEndOfResponse);
                                _currentResponseBufferReadPosition += chunkSize;
                            }
                        }
                    }
                }

                // if llm stream task is complete
                if (isEndOfResponse)
                {
                    if (currentTurn.Response.Type == ConversationTurnAgentResponseType.SystemTool || currentTurn.Response.Type == ConversationTurnAgentResponseType.CustomTool)
                    {
                        currentTurn.Response.LLMStreamingCompletedAt = DateTime.UtcNow;
                        currentTurn.Response.ToolExecution!.RawLLMInput = _responseBuffer.ToString();
                        await _conversationSession.NotifyTurnUpdated(currentTurn);

                        if (currentTurn.Response.Type == ConversationTurnAgentResponseType.SystemTool)
                        {
                            _logger.LogDebug("Agent {AgentId}: LLM streaming completed for response turn {TurnId}, executing system tool.", _agentState.AgentId, currentTurn.Id);

                            await SystemToolExecutionRequested?.Invoke(currentTurn);
                        }
                        else
                        {
                            _logger.LogDebug("Agent {AgentId}: LLM streaming completed for response turn {TurnId}, executing custom tool.", _agentState.AgentId, currentTurn.Id);

                            await CustomToolExecutionRequested?.Invoke(currentTurn);
                        }

                        return;
                    }
                    else if (currentTurn.Response.Type == ConversationTurnAgentResponseType.Speech)
                    {
                        currentTurn.Response.LLMStreamingCompletedAt = DateTime.UtcNow;
                        await _conversationSession.NotifyTurnUpdated(currentTurn);

                        _logger.LogDebug("Agent {AgentId}: LLM streaming completed for response turn {TurnId}, speech text generation is completed.", _agentState.AgentId, currentTurn.Id);
                        // todo, check remaning buffer?

                        return;
                    }
                    else
                    {
                        _logger.LogError("Agent {AgentId}: LLM response ended but type unknown or invalid: {Response}", _agentState.AgentId, _responseBuffer.ToString());
                        // TODO we should never reach here tbh, we have a check done above already when setting the response type
                        return;
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