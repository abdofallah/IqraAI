using IqraCore.Entities.Business;
using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.Conversation;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.Script;
using IqraInfrastructure.Managers.STT;
using IqraInfrastructure.Managers.TTS;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace IqraInfrastructure.Managers.Conversation
{
    public class ConversationAIAgent : IConversationAgent
    {  
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ConversationAIAgent> _logger;

        private readonly ConversationSessionManager _conversationSessionManager;

        private readonly BusinessManager _businessManager;
        private readonly SystemPromptGenerator _systemPromptGenerator;   
        private readonly STTProviderManager _sttProviderManager;
        private readonly TTSProviderManager _ttsProviderManager;
        private readonly LLMProviderManager _llmProviderManager;

        private readonly string _agentId;  

        private ConversationAgentConfiguration _agentConfiguration;
        private BusinessApp _businessApp;
        private BusinessAppRoute _currentSessionRoute;
        private BusinessAppAgent _businessAppAgent;
        private string _currentLanguageCode;

        private ScriptExecutionManager _scriptExecutionManager;

        private BusinessAppIntegration _sttBusinessIntegrationData;
        private ISTTService _sttService;

        private BusinessAppIntegration _ttsBusinessIntegrationData;
        private ITTSService _ttsService;

        private BusinessAppIntegration _llmBusinessIntegrationData;
        private ILLMService _llmService;

        private CancellationTokenSource _conversationCTS;
        private bool _isInitialized;

        private bool _isProcessingAudio;
        private bool _isAcceptingSTTAudio = false;
        private readonly BlockingCollection<byte[]> _audioQueue = new();
        private Task? _audioProcessingTask;
        private readonly Dictionary<string, string> _clientContextMap = new();
        private string? _currentClientId;

        private CancellationTokenSource _currentTTSProcessingTaskCTS = new();
        private Task<(byte[]?, TimeSpan?)>? TTSTask = null;

        private CancellationTokenSource _currentLLMProcessingTaskCTS = new();
        private Task? LLMTask = null;
        public string AgentId => _agentId;
        public ConversationAgentType AgentType => ConversationAgentType.AI;

        public event EventHandler<ConversationAudioGeneratedEventArgs>? AudioGenerated;
        public event EventHandler<ConversationTextGeneratedEventArgs>? TextGenerated;
        public event EventHandler<ConversationAgentThinkingEventArgs>? Thinking;
        public event EventHandler<ConversationAgentErrorEventArgs>? ErrorOccurred;

        public ConversationAIAgent(
            ILoggerFactory loggerFactory,
            ConversationSessionManager sessionManager,
            string agentId,
            BusinessManager businessManager,
            SystemPromptGenerator systemPromptGenerator,
            STTProviderManager sttProviderManager,
            TTSProviderManager ttsProviderManager,
            LLMProviderManager llmProviderManager
        )
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<ConversationAIAgent>();

            _conversationSessionManager = sessionManager;

            _businessManager = businessManager;
            _systemPromptGenerator = systemPromptGenerator;
            _sttProviderManager = sttProviderManager;
            _ttsProviderManager = ttsProviderManager;
            _llmProviderManager = llmProviderManager;

            _agentId = agentId;        
        }

        // Initalization
        public async Task InitializeAsync(ConversationAgentConfiguration config, BusinessApp businessAppData, BusinessAppRoute businessRouteData, CancellationToken cancellationToken)
        {
            if (_isInitialized)
            {
                _logger.LogWarning("AI Agent {AgentId} is already initialized", _agentId);
                return;
            }
            try
            {
                _conversationCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                _agentConfiguration = config;
                _businessApp = businessAppData;
                _currentSessionRoute = businessRouteData;   

                _businessAppAgent = await _businessManager.GetAgentsManager().GetAgentById(_agentConfiguration.BusinessId, _currentSessionRoute.Agent.SelectedAgentId);
                if (_businessAppAgent == null)
                {
                    _logger.LogError("Business app agent {AgentId} not found", _currentSessionRoute.Agent.SelectedAgentId);
                    ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Business app agent not found"));
                    return;
                }

                _currentLanguageCode = _currentSessionRoute.Language.DefaultLanguageCode;

                var defaultSTTService = _businessAppAgent.Integrations.STT[_currentLanguageCode][0];
                var defaultTTSService = _businessAppAgent.Integrations.TTS[_currentLanguageCode][0];
                var defaultLLMService = _businessAppAgent.Integrations.LLM[_currentLanguageCode][0];

                var sttBusinessIntegrationData = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(_agentConfiguration.BusinessId, defaultSTTService.Id);
                if (!sttBusinessIntegrationData.Success || sttBusinessIntegrationData.Data == null)
                {
                    _logger.LogError("Business app STT integration {IntegrationId} not found", defaultSTTService.Id);
                    ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Business app STT integration not found"));
                    return;
                }
                _sttBusinessIntegrationData = sttBusinessIntegrationData.Data;
                var sttServiceResult = await _sttProviderManager.BuildProviderServiceByIntegration(_sttBusinessIntegrationData, defaultSTTService, new Dictionary<string, string> { { "language", _currentLanguageCode } });
                if (!sttServiceResult.Success || sttServiceResult.Data == null)
                {
                    _logger.LogError("Failed to build STT service for agent {AgentId} with error: {ErrorMessage}", _agentId, sttServiceResult.Message);
                    ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Failed to build STT service for agent with error: " + sttServiceResult.Message));
                    return;
                }
                _sttService = sttServiceResult.Data;

                var ttsBusinessIntegrationData = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(_agentConfiguration.BusinessId, defaultTTSService.Id);
                if (!ttsBusinessIntegrationData.Success || ttsBusinessIntegrationData.Data == null)
                {
                    _logger.LogError("Business app TTS integration {IntegrationId} not found", defaultTTSService.Id);
                    ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Business app TTS integration not found"));
                    return;
                }
                _ttsBusinessIntegrationData = ttsBusinessIntegrationData.Data;
                var ttsServiceResult = await _ttsProviderManager.BuildProviderServiceByIntegration(_ttsBusinessIntegrationData, defaultTTSService, new Dictionary<string, string> { });
                if (!ttsServiceResult.Success || ttsServiceResult.Data == null)
                {
                    _logger.LogError("Failed to build TTS service for agent {AgentId} with error: {ErrorMessage}", _agentId, ttsServiceResult.Message);
                    ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Failed to build TTS service for agent with error: " + ttsServiceResult.Message));
                    return;
                }
                _ttsService = ttsServiceResult.Data;
                _sttService.TranscriptionResultReceived += OnTranscriptionResultReceived;
                _sttService.OnRecoginizingRecieved += OnRecognizingReceived;

                var llmBusinessIntegrationData = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(_agentConfiguration.BusinessId, defaultLLMService.Id);
                if (!llmBusinessIntegrationData.Success)
                {
                    _logger.LogError("Business app LLM integration {IntegrationId} not found", defaultLLMService.Id);
                    ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Business app LLM integration not found"));
                    return;
                }
                _llmBusinessIntegrationData = llmBusinessIntegrationData.Data;
                var llmServiceResult = await _llmProviderManager.BuildProviderServiceByIntegration(_llmBusinessIntegrationData, defaultLLMService, new Dictionary<string, string> { });
                if (!llmServiceResult.Success || llmServiceResult.Data == null)
                {
                    _logger.LogError("Failed to build LLM service for agent {AgentId} with error: {ErrorMessage}", _agentId, llmServiceResult.Message);
                    ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Failed to build LLM service for agent with error: " + llmServiceResult.Message));
                    return;
                }
                _llmService = llmServiceResult.Data;
                _llmService.MessageStreamed += OnLLMMessageStreamed;  

                // Initialize services
                _sttService.Initialize();
                _sttService.StartTranscription();

                _ttsService.Initialize();

                // Set up LLM with system prompt
                await InitalizePromptAndScriptAsync();

                // Start audio processing task
                _isProcessingAudio = true;
                _audioProcessingTask = Task.Run(ProcessAudioQueueAsync, _conversationCTS.Token);

                _isInitialized = true;
                _logger.LogInformation("AI Agent {AgentId} initialized with business ID {BusinessId}, route {RouteId}", _agentId, _agentConfiguration.BusinessId, _agentConfiguration.RouteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing AI Agent {AgentId}", _agentId);
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error initializing agent: " + ex.Message, ex));
                throw;
            }
        }
        private async Task InitalizePromptAndScriptAsync()
        {
            try
            {
                // Generate system prompt
                var systemPromptResult = await _systemPromptGenerator.GenerateInitialSystemPrompt(
                    _businessApp,
                    _businessAppAgent,
                    _currentSessionRoute,
                    _currentLanguageCode,
                    _llmService.GetProviderType(),
                    _llmService.GetModel(),
                    _conversationSessionManager.PrimaryClientIdentifier()
                );

                if (!systemPromptResult.Success || systemPromptResult.Data == null)
                {
                    _logger.LogError("Error generating system prompt for AI Agent {AgentId}: {Code} {Message}", _agentId, systemPromptResult.Code, systemPromptResult.Message);
                    ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error generating system prompt: " + systemPromptResult.Code + " " + systemPromptResult.Message));
                    return;
                }

                // Configure LLM
                _llmService.SetSystemPrompt(systemPromptResult.Data);

                // Get script from route
                _scriptExecutionManager = new ScriptExecutionManager(
                    _loggerFactory.CreateLogger<ScriptExecutionManager>()
                );
                await _scriptExecutionManager.LoadScriptAsync(
                    _businessApp,
                    _currentSessionRoute,
                    _currentLanguageCode
                );     

                _logger.LogInformation("Configured LLM for AI Agent {AgentId} with business data", _agentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring LLM with business data for AI Agent {AgentId}", _agentId);
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error configuring agent: " + ex.Message, ex));
                throw;
            }
        }

        // Notifications
        public async Task NotifyConversationStarted()
        {
            _llmService.AddUserMessage("response_from_system: Call has started.");

            if (_businessAppAgent.Utterances.OpeningType == BusinessAppAgentOpeningType.AgentFirst)
            {
                string openingMessage = _businessAppAgent.Utterances.GreetingMessage[_currentLanguageCode];

                _llmService.AddAssistantMessage(openingMessage);
                var speakSpan = await SynthesizeSpeechAsync(openingMessage);

                await Task.Delay((int)speakSpan.TotalMilliseconds);
            }
            else
            {
                _llmService.AddAssistantMessage("execute_system_function: acknowledge(\"Call Start\")");
            }

            _isAcceptingSTTAudio = true;
        }

        public async Task NotifyMaxDurationReached()
        {
            await CancelOnGoingAgentProcessingTask();
            _isAcceptingSTTAudio = false;

            _llmService.AddUserMessage($"response_from_system: Perform execute_system_function: end_call(...) right away while notifying the user taht maximum duration of {_currentSessionRoute.Configuration.MaxCallTimeS} seconds has been reached for the call.");
            await _llmService.ProcessInputAsync(_conversationCTS.Token);
        }

        // Take Audio and Text Input from Conversation Manager
        public async Task ProcessAudioAsync(byte[] audioData, string clientId, CancellationToken cancellationToken)
        {
            if (!_isInitialized)
            {
                _logger.LogWarning("Cannot process audio because AI Agent {AgentId} is not initialized", _agentId);
                return;
            }

            try
            {
                // Set current client context
                _currentClientId = clientId;

                // Add audio to processing queue
                _audioQueue.Add(audioData, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Normal cancellation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queuing audio for processing in AI Agent {AgentId}", _agentId);
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error processing audio: " + ex.Message, ex));
            }
        }
        public async Task ProcessTextAsync(string text, string? clientId, CancellationToken cancellationToken)
        {
            if (!_isInitialized)
            {
                _logger.LogWarning("Cannot process text because AI Agent {AgentId} is not initialized", _agentId);
                return;
            }

            try
            {
                bool isFillerWord = WordsNotToCancelCurrentProcessing.Contains(text.Trim().ToLower());
                if (_isExecutingSystemTool || _isExecutingCustomTool)
                {
                    if (isFillerWord) return; // we can ignore the filler word as it makes no sense for now

                    // AI is busy executing the tool so we will ask the user to wait
                    // Add some kind of either ai processing that allows ai to take the text
                    // and tell if we should force cancel the tool execution or ignore user query for now

                    // OR for now let user speak (A moment as I finish this task)
                    return;
                }

                if (_isResponding)
                {
                    if (isFillerWord) return; // we can ignore this filler word if the ai is still responding

                    await CancelOnGoingAgentProcessingTask();

                    if (_responseBuffer.Length > 0)
                    {
                        var responseReadSoFar = _responseBuffer.ToString().Substring(_currentResponseBufferRead);
                        _llmService.AddAssistantMessage(responseReadSoFar + "...");

                    }

                    _isResponding = false;
                    _responseBuffer.Clear();
                    _currentResponseBufferRead = 0;
                }

                var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_currentLLMProcessingTaskCTS.Token, _conversationCTS.Token, cancellationToken).Token;

                _currentClientId = clientId;
                if (clientId != null)
                {
                    _clientContextMap[clientId] = text;
                }

                _llmService.AddUserMessage($"customer_query: {text}");
                LLMTask = _llmService.ProcessInputAsync(combinedCancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing text in AI Agent {AgentId}", _agentId);
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error processing text: " + ex.Message, ex));
            }
        }

        // Inner Agent Processing
        private async Task ProcessAudioQueueAsync()
        {
            try
            {
                while (_isProcessingAudio && !_conversationCTS!.Token.IsCancellationRequested)
                {
                    if (_audioQueue.TryTake(out var audioData, 10, _conversationCTS.Token))
                    {
                        if (_isAcceptingSTTAudio)
                        {
                            _sttService.WriteTranscriptionAudioData(audioData);
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (_conversationCTS!.Token.IsCancellationRequested)
            {
                // Normal cancellation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in audio processing task for AI Agent {AgentId}", _agentId);
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error processing audio queue: " + ex.Message, ex));
            }
        }
        private void OnRecognizingReceived(object? sender, object e)
        {
            // do nothing for now
        }
        private static List<string> WordsNotToCancelCurrentProcessing = new List<string>
        { // TODO this is temporary add this into the agent settings or routing
            "alright",
            "right",
            "mm hmm",
            "mm",
            "huh",
            "hmm",
            "ok",
            "okay",
            "aha",
            "ahan",
            "really",
            "wow",
            "great",
            "good"
        };
        private async void OnTranscriptionResultReceived(object? sender, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            try
            {
                _logger.LogInformation("AI Agent {AgentId} received transcription: {Text}", _agentId, text);
                await ProcessTextAsync(text, _currentClientId, (new CancellationTokenSource()).Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling transcription result");
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error handling transcription: " + ex.Message, ex));
            }
        }

        private readonly SemaphoreSlim _llmResponseLock = new SemaphoreSlim(1, 1);
        private async void OnLLMMessageStreamed(object? sender, object responseObj)
        {
            await _llmResponseLock.WaitAsync();

            try
            {
                string? deltaText = null;
                bool isEndOfResponse = false;
                string fullAggregatedMessage = "";

                if (_llmService.GetProviderType() == InterfaceLLMProviderEnum.AnthropicClaude)
                {
                    var response = (Anthropic.SDK.Messaging.MessageResponse)responseObj;
                    if (response.Delta != null)
                    {
                        deltaText = response.Delta.Text;
                        fullAggregatedMessage = deltaText;

                        if (
                            response.Delta != null &&
                            response.Delta.StopReason != null &&
                            (response.Delta.StopReason == "max_tokens" || response.Delta.StopReason != "end_turn")
                        )
                        {
                            isEndOfResponse = true;
                        }
                    }
                }
                else if (_llmService.GetProviderType() == InterfaceLLMProviderEnum.OpenAIGPT)
                {
                    var response = (OpenAI.Chat.StreamingChatCompletionUpdate)responseObj;

                    deltaText = response.ContentUpdate.ToString();

                    if (
                        response.FinishReason != null &&
                        (response.FinishReason == OpenAI.Chat.ChatFinishReason.Stop || response.FinishReason == OpenAI.Chat.ChatFinishReason.Length)
                    )
                    {
                        isEndOfResponse = true;
                    }
                }
                else if (_llmService.GetProviderType() == InterfaceLLMProviderEnum.GoogleAIGemini)
                {
                    var response = (GenerativeAI.Types.GenerateContentResponse)responseObj;

                    var candidate = response.Candidates.FirstOrDefault(); // Usually only one candidate
                    if (candidate?.Content?.Parts?.FirstOrDefault() != null)
                    {
                        deltaText = candidate.Content.Parts.First().Text;
                    }

                    if (candidate != null &&
                        candidate.FinishReason != null &&
                        candidate.FinishReason != (GenerativeAI.Types.FinishReason.FINISH_REASON_UNSPECIFIED)
                    )
                    {
                        isEndOfResponse = true;
                    }
                }
                else
                {
                    _logger.LogError("Unhandled LLM provider type: {ProviderType}", _llmService.GetProviderType());
                    throw new NotImplementedException($"LLM provider type {_llmService.GetProviderType()} not implemented in OnLLMMessageStreamed");
                }

                if (!string.IsNullOrEmpty(deltaText))
                {
                    _responseBuffer.Append(deltaText);
                }      

                // Figure out what kind of response this is
                if (!_isResponding && !_isExecutingSystemTool && !_isExecutingCustomTool)
                {
                    var fullText = _responseBuffer.ToString();
                    if (fullText.StartsWith("response_to_customer:"))
                    {
                        _isResponding = true;
                        _currentResponseBufferRead = 21;
                    }
                    else if (fullText.StartsWith("execute_system_function:"))
                    {
                        _isExecutingSystemTool = true;
                        return;
                    }
                    else if (fullText.StartsWith("execute_custom_function:"))
                    {
                        _isExecutingCustomTool = true;
                        return;
                    }
                }

                // Handle responding tasks
                if (_isResponding)
                {
                    await HandleLLMResponseProcessingAsync(deltaText);
                }

                // Handle complete result tasks
                if (isEndOfResponse)
                {
                    if (_isResponding)
                    {
                        await HandleLLMResponseCompletedAsync();
                    }
                    else if (_isExecutingSystemTool)
                    {
                        await HandleLLMSystemToolResponseCompletedAsync();
                    }
                    else if (_isExecutingCustomTool)
                    {
                        await HandleLLMCustomToolResponseCompletedAsync();
                    }
                    else
                    {
                        _logger.LogError("Agent {AgentId} Unhandled LLM response: {Response}", _agentId, fullAggregatedMessage);
                        _llmService.AddUserMessage("response_from_system: Invalid response type recieved from the agent, the agent must always begin their response with response_to_customer or execute_system_function or execute_custom_function.");

                        _responseBuffer.Clear();
                        _isResponding = false;
                        _isExecutingCustomTool = false;
                        _isExecutingSystemTool = false;

                        await _llmService.ProcessInputAsync(_conversationCTS?.Token ?? CancellationToken.None);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing LLM streaming response");
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error processing LLM response: " + ex.Message, ex));
            }
            finally
            {
                _llmResponseLock.Release();
            }
        }

        private StringBuilder _responseBuffer = new StringBuilder();
        private int _currentResponseBufferRead = 0;

        private bool _isResponding = false;
        private bool _isExecutingSystemTool = false;
        private bool _isExecutingCustomTool = false;

        // Handle Agent Normal Response
        private async Task HandleLLMResponseProcessingAsync(string deltaText)
        {
            if (string.IsNullOrEmpty(deltaText)) return;

            string unprocessedText = _responseBuffer.ToString().Substring(_currentResponseBufferRead);
            if (unprocessedText.Length == 0) return;

            bool isCompleteSentence = unprocessedText.EndsWith(".") || unprocessedText.EndsWith("!") || unprocessedText.EndsWith("?");
            bool isLargeChunk = unprocessedText.Length > 100;

            if ((isCompleteSentence && unprocessedText.Length > 10) || isLargeChunk)
            {
                string textToSynthesize;
                int chunkSize;

                if (!isCompleteSentence && isLargeChunk)
                {
                    // Find the last sentence boundary to create a clean cut
                    int lastIndex = -1;
                    foreach (char punctuation in new[] { '.', '!', '?', ',' })
                    {
                        int index = unprocessedText.LastIndexOf(punctuation);
                        if (index > lastIndex)
                        {
                            lastIndex = index;
                        }
                    }

                    if (lastIndex > 0)
                    {
                        // Split at the last sentence boundary
                        textToSynthesize = unprocessedText.Substring(0, lastIndex + 1);
                        chunkSize = lastIndex + 1;
                    }
                    else
                    {
                        // No good split point found, use the whole chunk
                        textToSynthesize = unprocessedText;
                        chunkSize = unprocessedText.Length;
                    }
                }
                else
                {
                    // It's a complete sentence or we're handling a large chunk as-is
                    textToSynthesize = unprocessedText;
                    chunkSize = unprocessedText.Length;
                }

                // Synthesize the text and update the read position precisely
                await SynthesizeSpeechAsync(textToSynthesize);
                TextGenerated?.Invoke(this, new ConversationTextGeneratedEventArgs(textToSynthesize, _currentClientId));
                _currentResponseBufferRead += chunkSize;
            }
        }
        private async Task HandleLLMResponseCompletedAsync()
        {
            try
            {
                var completeText = _responseBuffer.ToString();

                // Handle any remaining text in the response section
                if (_isResponding && _currentResponseBufferRead < _responseBuffer.Length)
                {
                    var remainingText = completeText.Substring(_currentResponseBufferRead);
                    await SynthesizeSpeechAsync(remainingText);
                    TextGenerated?.Invoke(this, new ConversationTextGeneratedEventArgs(remainingText, _currentClientId));
                }

                _llmService.AddAssistantMessage(completeText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling LLM response completion");
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error completing response: " + ex.Message, ex));
            }
            finally
            {
                _responseBuffer.Clear();
                _currentResponseBufferRead = 0;
                _isResponding = false;
            }
        }

        // Handle Agent System Tool Response
        private async Task HandleLLMSystemToolResponseCompletedAsync()
        {
            var llmResponseFull = _responseBuffer.ToString();

            _logger.LogInformation("AI Agent {AgentId} received system tool response: {Response}", _agentId, llmResponseFull);

            try
            {
                int startFunctionIndex = llmResponseFull.IndexOf("execute_system_function:");
                if (startFunctionIndex != -1)
                {
                    _llmService.AddAssistantMessage(llmResponseFull);

                    // Extract content after execute_system_function:
                    int startContentIndex = startFunctionIndex + "execute_system_function:".Length;
                    string functionContent = llmResponseFull.Substring(startContentIndex).Trim();

                    // Check which system tool is being called
                    if (functionContent.StartsWith("end_call:"))
                    {
                        string argsContent = functionContent.Substring("end_call:".Length).Trim();

                        // Parse arguments using our CSV-like approach
                        List<string> arguments = ParseArguments(argsContent);

                        string reasonForEnding = "";
                        string? messageToSpeak = null;

                        if (arguments.Count >= 1)
                        {
                            // First argument is the reason
                            var reasonArg = arguments[0];
                            reasonForEnding = reasonArg.Equals("null", StringComparison.OrdinalIgnoreCase) ? "" :
                                             reasonArg.Trim().Trim('"');

                            // Second argument is the optional message
                            if (arguments.Count >= 2)
                            {
                                var messageArg = arguments[1];
                                messageToSpeak = messageArg.Equals("null", StringComparison.OrdinalIgnoreCase) ||
                                                 messageArg.Trim() == "\"\"" ?
                                                 null : messageArg.Trim().Trim('"');
                            }
                        }

                        _logger.LogInformation("AI Agent {AgentId} received end_call: {ReasonForEnding} and {MessageToSpeak}",
                                              _agentId, reasonForEnding, messageToSpeak);

                        if (!string.IsNullOrWhiteSpace(messageToSpeak))
                        {
                            var durationToWait = await SynthesizeSpeechAsync(messageToSpeak);
                            if (durationToWait != TimeSpan.Zero)
                            {
                                await Task.Delay((int)durationToWait.TotalMilliseconds);
                            }
                            else
                            {
                                // CRITICAL SHOULD NOT HAPPEN
                                _logger.LogError("Ending call but the audio did not have any duration to wait for");
                            }
                        }

                        await _conversationSessionManager.EndAsync(reasonForEnding);
                        return;
                    }

                    else if (functionContent.StartsWith("transfer_to_agent:"))
                    {
                        // Handle transfer_to_agent
                        // Implement similar parsing logic as above
                        return;
                    }

                    else if (functionContent.StartsWith("receive_dtmf_input:"))
                    {
                        // Handle receive_dtmf_input
                        // Implement similar parsing logic as above
                        return;
                    }
                    else if (functionContent.StartsWith("retrieve_product_information:"))
                    {
                        // Handle retrieve_product_information
                        // Implement similar parsing logic as above
                        return;
                    }
                    else if (functionContent.StartsWith("execute_custom_function:"))
                    {
                        // Handle execute_custom_function
                        // Implement similar parsing logic as above
                        return;
                    }

                    else
                    {
                        _llmService.AddUserMessage("response_from_system: Invalid system tool response received. Unknown tool name. The system is only expecting end_call, transfer_to_agent, or receive_dtmf_input system tool in the format of [execute_system_function: toolName: arg1, arg2, ...]");
                        await _llmService.ProcessInputAsync(_conversationCTS?.Token ?? CancellationToken.None);
                        return;
                    }
                }

                _llmService.AddUserMessage("response_from_system: Invalid system tool response received. The system is only expecting end_call, transfer_to_agent, or receive_dtmf_input system tool in the format of [execute_system_function: toolName: arg1, arg2, ...]");
                await _llmService.ProcessInputAsync(_conversationCTS?.Token ?? CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing system function, {Response}", llmResponseFull);
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error executing system function: " + ex.Message, ex));
                _llmService.AddUserMessage("response_from_system: There was an internal system error when trying to run the system tool...");
                await _llmService.ProcessInputAsync(_conversationCTS?.Token ?? CancellationToken.None);
            }
            finally
            {
                _responseBuffer.Clear();
                _currentResponseBufferRead = 0;
                _isExecutingSystemTool = false;
            }
        }

        // Handle Agent Custom Tool Response
        private async Task HandleLLMCustomToolResponseCompletedAsync()
        {
            var llmResponseFull = _responseBuffer.ToString();

            _logger.LogInformation("AI Agent {AgentId} received custom tool response: {Response}", _agentId, llmResponseFull);

            try
            {
                int startFunctionIndex = llmResponseFull.IndexOf("execute_custom_function:");
                if (startFunctionIndex != -1)
                {
                    _llmService.AddAssistantMessage(llmResponseFull);

                    // Extract everything after the colon
                    int startContentIndex = startFunctionIndex + "execute_custom_function:".Length;
                    string functionContent = llmResponseFull.Substring(startContentIndex).Trim();

                    // Use the shared ParseArguments method
                    List<string> arguments = ParseArguments(functionContent);

                    string reasonForExecuting = "";
                    string nodeIdToExecute = "";
                    string? nodeVariableValues = null;

                    if (arguments.Count >= 1)
                    {
                        // First argument is the reason
                        string reasonArg = arguments[0];
                        reasonForExecuting = reasonArg.Equals("null", StringComparison.OrdinalIgnoreCase) ? "" :
                                            reasonArg.Trim().Trim('"');

                        // Second argument is the node id to execute
                        if (arguments.Count >= 2)
                        {
                            string nodeIdArg = arguments[1];
                            nodeIdToExecute = nodeIdArg.Equals("null", StringComparison.OrdinalIgnoreCase) ? "" :
                                             nodeIdArg.Trim().Trim('"');

                            // Third argument is the node variables (JSON object)
                            if (arguments.Count >= 3)
                            {
                                string nodeVarsArg = arguments[2];
                                if (!nodeVarsArg.Equals("null", StringComparison.OrdinalIgnoreCase) &&
                                    nodeVarsArg.Trim().StartsWith("{") && nodeVarsArg.Trim().EndsWith("}"))
                                {
                                    nodeVariableValues = nodeVarsArg.Trim();
                                }
                            }
                        }
                    }

                    Dictionary<string, JsonElement>? nodeVariables = null;
                    if (!string.IsNullOrEmpty(nodeVariableValues))
                    {
                        try
                        {
                            nodeVariables = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(nodeVariableValues);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error parsing custom tool response, Invalid custom tool response received, {Response}", llmResponseFull);
                            _llmService.AddUserMessage("response_from_system: Invalid custom tool response received. The system could not parse the JSON variables values for the custom tool provided. The custom tool extracted variables values must be in string JSON object format: [execute_custom_function: \"reason\", \"node id\", { \"var1\": variablevalue }]");
                            await _llmService.ProcessInputAsync(_conversationCTS?.Token ?? CancellationToken.None);
                            return;
                        }
                    }

                    var executeCustomToolResult = await _scriptExecutionManager.ExecuteCustomToolAsync(nodeIdToExecute, nodeVariables ?? new Dictionary<string, JsonElement>());
                    if (!executeCustomToolResult.Success)
                    {
                        _logger.LogError("Error executing custom tool, Failed to execute custom tool, {Response}", executeCustomToolResult.Data);
                        _llmService.AddUserMessage("response_from_system: Failed to execute custom tool: " + executeCustomToolResult.Message);
                        await _llmService.ProcessInputAsync(_conversationCTS?.Token ?? CancellationToken.None);
                        return;
                    }

                    _logger.LogInformation("Agent {AgentId} executed successfully custom tool with response: {Response}", _agentId, executeCustomToolResult.Data);
                    _llmService.AddUserMessage("response_from_system: Successfully executed custom tool: Response: " + executeCustomToolResult.Data);
                    await _llmService.ProcessInputAsync(_conversationCTS?.Token ?? CancellationToken.None);
                    return;
                }

                _logger.LogError("Error executing custom tool, Invalid custom tool response received, {Response}", llmResponseFull);
                _llmService.AddUserMessage("response_from_system: Invalid custom tool response received. The system is only expecting custom tool in the format of [execute_custom_function: \"reason\", \"node id\", { \"var1\": variablevalue }]");
                await _llmService.ProcessInputAsync(_conversationCTS?.Token ?? CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing system function, {Response}", llmResponseFull);
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error executing system function: " + ex.Message, ex));
                _llmService.AddUserMessage("response_from_system: There was an internal system error when trying to run the system tool...");
                await _llmService.ProcessInputAsync(_conversationCTS?.Token ?? CancellationToken.None);
            }
            finally
            {
                _responseBuffer.Clear();
                _currentResponseBufferRead = 0;
                _isExecutingCustomTool = false;
            }
        }

        // Agent Speaking Functions
        private async Task<TimeSpan> SynthesizeSpeechAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return TimeSpan.Zero;

            try
            {
                var combinedCancellatinToken = CancellationTokenSource.CreateLinkedTokenSource(_conversationCTS.Token, _currentTTSProcessingTaskCTS.Token).Token;

                TTSTask = _ttsService.SynthesizeTextAsync(text, combinedCancellatinToken);
                var (audioData, audioDuration) = await TTSTask;
                AudioGenerated?.Invoke(this, new ConversationAudioGeneratedEventArgs(audioData, _currentClientId));
                return audioDuration ?? TimeSpan.Zero;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error synthesizing speech for text: {Text}", text);
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error synthesizing speech: " + ex.Message, ex));

                return TimeSpan.Zero;
            }
        }

        // Agent Response Cancellation
        private async Task CancelOnGoingAgentProcessingTask()
        {
            _isAcceptingSTTAudio = false;
            _sttService.StopTranscription();
            await _ttsService.StopTextSynthesisAsync();

            _currentTTSProcessingTaskCTS.Cancel();
            _currentLLMProcessingTaskCTS.Cancel();
            if (LLMTask != null)
            {
                LLMTask.Wait(1000);
            }
            if (TTSTask != null)
            {
                TTSTask.Wait(1000);
            }
            _currentTTSProcessingTaskCTS = new CancellationTokenSource();
            _currentLLMProcessingTaskCTS = new CancellationTokenSource();

            _audioQueue.TryTake(out _);

            _isAcceptingSTTAudio = true;
        }

        // Controls To End Agent Life
        public async Task ShutdownAsync(string reason)
        {
            if (!_isInitialized)
            {
                _logger.LogInformation("AI Agent {AgentId} is already shut down", _agentId);
                return;
            }

            try
            {
                // Stop processing
                _isProcessingAudio = false;
                _conversationCTS?.Cancel();
                _currentTTSProcessingTaskCTS?.Cancel();
                _currentLLMProcessingTaskCTS?.Cancel();

                // Clean up resources
                _sttService.StopTranscription();
                _sttService.TranscriptionResultReceived -= OnTranscriptionResultReceived;
                _sttService.OnRecoginizingRecieved -= OnRecognizingReceived;
                _llmService.MessageStreamed -= OnLLMMessageStreamed;

                _audioQueue.CompleteAdding();

                // Wait for tasks to complete
                var tasksToWait = new List<Task>();
                if (_audioProcessingTask != null)
                {
                    tasksToWait.Add(Task.WhenAny(_audioProcessingTask, Task.Delay(1000)));
                }
                if (LLMTask != null)
                {
                    tasksToWait.Add(Task.WhenAny(LLMTask, Task.Delay(1000)));
                }
                if (TTSTask != null)
                {
                    tasksToWait.Add(Task.WhenAny(TTSTask, Task.Delay(1000)));
                }
                await Task.WhenAll(tasksToWait);

                _conversationCTS?.Dispose();
                _currentTTSProcessingTaskCTS?.Dispose();
                _currentLLMProcessingTaskCTS?.Dispose();

                _isInitialized = false;
                _logger.LogInformation("AI Agent {AgentId} shut down: {Reason}", _agentId, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error shutting down AI Agent {AgentId}", _agentId);
            }
        }

        // Helper
        // Shared helper method for parsing arguments with proper handling of quotes and JSON objects
        private List<string> ParseArguments(string input)
        {
            List<string> arguments = new List<string>();
            int currentPos = 0;
            int depth = 0;
            StringBuilder currentArg = new StringBuilder();
            bool inQuotes = false;

            while (currentPos < input.Length)
            {
                char c = input[currentPos];

                if (c == '"' && (currentPos == 0 || input[currentPos - 1] != '\\'))
                {
                    inQuotes = !inQuotes;
                    currentArg.Append(c);
                }
                else if (c == '{' && !inQuotes)
                {
                    depth++;
                    currentArg.Append(c);
                }
                else if (c == '}' && !inQuotes)
                {
                    depth--;
                    currentArg.Append(c);
                }
                else if (c == ',' && !inQuotes && depth == 0)
                {
                    // End of argument
                    arguments.Add(currentArg.ToString().Trim());
                    currentArg.Clear();
                }
                else
                {
                    currentArg.Append(c);
                }

                currentPos++;
            }

            // Add the last argument if there's anything
            if (currentArg.Length > 0)
            {
                arguments.Add(currentArg.ToString().Trim());
            }

            return arguments;
        }
    }
}