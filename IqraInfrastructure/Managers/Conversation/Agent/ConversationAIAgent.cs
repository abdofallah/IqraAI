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
using System.Diagnostics;
using System.Text;

namespace IqraInfrastructure.Managers.Conversation
{
    public class ConversationAIAgent : IConversationAgent
    {  
        private readonly ILogger<ConversationAIAgent> _logger;

        private readonly ConversationSessionManager _conversationSessionManager;

        private readonly BusinessManager _businessManager;
        private readonly SystemPromptGenerator _systemPromptGenerator;
        private readonly ScriptExecutionManager _scriptExecutionManager;
        private readonly STTProviderManager _sttProviderManager;
        private readonly TTSProviderManager _ttsProviderManager;
        private readonly LLMProviderManager _llmProviderManager;

        private readonly string _agentId;  

        private ConversationAgentConfiguration _agentConfiguration;
        private BusinessApp _businessApp;
        private BusinessAppRoute _businessAppRoute;
        private BusinessAppAgent _businessAppAgent;
        private string _currentLanguageCode;

        private BusinessAppIntegration _sttBusinessIntegrationData;
        private ISTTService _sttService;

        private BusinessAppIntegration _ttsBusinessIntegrationData;
        private ITTSService _ttsService;

        private BusinessAppIntegration _llmBusinessIntegrationData;
        private ILLMService _llmService;

        private CancellationTokenSource _conversationCTS;
        private bool _isInitialized;

        private bool _isProcessingAudio;
        private bool _isAcceptingSTTAudio = true;
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
            ILogger<ConversationAIAgent> logger,
            ConversationSessionManager sessionManager,
            string agentId,
            BusinessManager businessManager,
            SystemPromptGenerator systemPromptGenerator,
            ScriptExecutionManager scriptExecutionManager,
            STTProviderManager sttProviderManager,
            TTSProviderManager ttsProviderManager,
            LLMProviderManager llmProviderManager
        )
        {
            _logger = logger;

            _conversationSessionManager = sessionManager;

            _businessManager = businessManager;
            _systemPromptGenerator = systemPromptGenerator;
            _scriptExecutionManager = scriptExecutionManager;
            _sttProviderManager = sttProviderManager;
            _ttsProviderManager = ttsProviderManager;
            _llmProviderManager = llmProviderManager;

            _agentId = agentId;        
        }

        public async Task InitializeAsync(ConversationAgentConfiguration config, BusinessApp businessAppData, BusinessAppRoute businessRouteData, CancellationToken cancellationToken)
        {
            if (_isInitialized)
            {
                _logger.LogWarning("AI Agent {AgentId} is already initialized", _agentId);
                return;
            }
            try
            {
                _agentConfiguration = config;
                _businessApp = businessAppData;
                _businessAppRoute = businessRouteData;   

                _businessAppAgent = await _businessManager.GetAgentsManager().GetAgentById(_agentConfiguration.BusinessId, _businessAppRoute.Agent.SelectedAgentId);
                if (_businessAppAgent == null)
                {
                    _logger.LogError("Business app agent {AgentId} not found", _businessAppRoute.Agent.SelectedAgentId);
                    ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Business app agent not found"));
                    return;
                }

                _currentLanguageCode = _businessAppRoute.Language.DefaultLanguageCode;

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

                _conversationCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                // Initialize services
                _sttService.Initialize();
                _sttService.StartTranscription();

                _ttsService.Initialize();

                // Set up LLM with system prompt
                await ConfigureLLMWithBusinessDataAsync();

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
                var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_currentLLMProcessingTaskCTS.Token, _conversationCTS.Token, cancellationToken).Token;

                _currentClientId = clientId;
                if (clientId != null)
                {
                    _clientContextMap[clientId] = text;
                }

                LLMTask = _llmService.ProcessInputAsync(text, combinedCancellationToken);
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

        private async Task ConfigureLLMWithBusinessDataAsync()
        {
            try
            {
                // Generate system prompt
                var systemPromptResult = await _systemPromptGenerator.GenerateInitialSystemPrompt(
                    _businessApp,
                    _businessAppAgent,
                    _businessAppRoute,
                    _currentLanguageCode,
                    _llmService.GetProviderType(),
                    _llmService.GetModel()
                );

                if (!systemPromptResult.Success || systemPromptResult.Data == null)
                {
                    _logger.LogError("Error generating system prompt for AI Agent {AgentId}: {Code} {Message}", _agentId, systemPromptResult.Code, systemPromptResult.Message);
                    ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error generating system prompt: " + systemPromptResult.Code + " " + systemPromptResult.Message));
                    return;
                }

                // Get script from route
                await _scriptExecutionManager.LoadScriptAsync(
                    _agentConfiguration.BusinessId,
                    _businessAppRoute.Agent.OpeningScriptId,
                    _currentLanguageCode
                );

                // Configure LLM
                _llmService.SetSystemPrompt(systemPromptResult.Data);

                // Get opening message if provided
                var openingMessage = _businessAppAgent.Utterances.GreetingMessage.GetValueOrDefault(_currentLanguageCode);
                if (!string.IsNullOrEmpty(openingMessage))
                {
                    _llmService.SetInitialMessage(openingMessage);
                }

                _logger.LogInformation("Configured LLM for AI Agent {AgentId} with business data", _agentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring LLM with business data for AI Agent {AgentId}", _agentId);
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error configuring agent: " + ex.Message, ex));
                throw;
            }
        }

        private async Task ProcessSystemCommandAsync(string command, CancellationToken cancellationToken)
        {
            try
            {
                if (command.StartsWith("<silence"))
                {
                    // Extract duration if present
                    double duration = 0;
                    var durationMatch = System.Text.RegularExpressions.Regex.Match(command, @"duration=""([0-9\.]+)""");
                    if (durationMatch.Success && durationMatch.Groups.Count > 1)
                    {
                        double.TryParse(durationMatch.Groups[1].Value, out duration);
                    }

                    _logger.LogInformation("Silence detected for {Duration}s, notifying LLM", duration);

                    // Send a nudge to the LLM to respond to silence
                    var inputText = $"The user has been silent for {duration} seconds. You should politely check if they're still there or if they need more information.";
                    await _llmService.ProcessInputAsync(inputText, cancellationToken);
                }
                else if (command.StartsWith("<dtmf"))
                {
                    // Extract DTMF digits
                    var dtmfMatch = System.Text.RegularExpressions.Regex.Match(command, @"<dtmf>(.*?)</dtmf>");
                    if (dtmfMatch.Success && dtmfMatch.Groups.Count > 1)
                    {
                        var digits = dtmfMatch.Groups[1].Value;
                        _logger.LogInformation("DTMF received: {Digits}", digits);

                        // Process DTMF through script engine if active
                        if (_scriptExecutionManager.IsScriptActive)
                        {
                            await _scriptExecutionManager.ProcessDTMFInputAsync(digits);
                        }
                        else
                        {
                            // Otherwise, pass to LLM
                            var inputText = $"The user pressed these keys on their phone keypad: {digits}";
                            await _llmService.ProcessInputAsync(inputText, cancellationToken);
                        }
                    }
                }
                // Add other system commands as needed
                // TODO
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing system command: {Command}", command);
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error processing system command: " + ex.Message, ex));
            }
        }

        #region Event Handlers

        private void OnRecognizingReceived(object? sender, object e)
        {
            // do nothing for now
        }

        private static List<string> WordsNotToCancelCurrentProcessing = new List<string>
        { // TODO this is temporary
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


                if (_isExecutingSystemTool || _isExecutingCustomTool)
                {
                    // TODO add the transaciption to the result

                    return;
                }

                bool isFillerWord = WordsNotToCancelCurrentProcessing.Contains(text.Trim().ToLower());
                if (_isResponding && isFillerWord)
                {
                    // TODO add the transaciption to the result

                    return;
                }

                await CancelOnGoingAgentProcessingTask();

                await ProcessTextAsync(text, _currentClientId, (new CancellationTokenSource()).Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling transcription result");
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error handling transcription: " + ex.Message, ex));
            }
        }

        private readonly SemaphoreSlim _responseLock = new SemaphoreSlim(1, 1);
        private async void OnLLMMessageStreamed(object? sender, object responseObj)
        {
            await _responseLock.WaitAsync();

            try
            {
                string? deltaText = null;

                if (_llmService.GetProviderType() == InterfaceLLMProviderEnum.AnthropicClaude)
                {
                    var response = (Anthropic.SDK.Messaging.MessageResponse)responseObj;
                    if (response.Delta != null)
                    {
                        deltaText = response.Delta.Text;

                        // Check for end of response
                        if (response.Delta.StopReason == "end_turn")
                        {
                            _llmService.AddAssistantMessage(deltaText);

                            if (_isResponding)
                            {
                                await HandleLLMResponseCompletedAsync();
                                return;
                            }

                            if (_isExecutingSystemTool)
                            {
                                await HandleLLMSystemToolResponseCompletedAsync();
                                return;
                            }
                        }
                    }
                }
                else if (_llmService.GetProviderType() == InterfaceLLMProviderEnum.OpenAIGPT)
                {
                    throw new NotImplementedException();
                    // TODO
                }
                else
                {
                    throw new NotImplementedException();
                }
                // TODO Add other LLM providers as needed

                if (!string.IsNullOrEmpty(deltaText))
                {
                    await ProcessLLMDeltaTextAsync(deltaText);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing LLM streaming response");
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error processing LLM response: " + ex.Message, ex));
            }
            finally
            {
                _responseLock.Release();
            }
        }

        private StringBuilder _responseBuffer = new StringBuilder();
        private int _currentResponseBufferRead = 0;
        private bool _isResponding = false;
        private bool _isExecutingSystemTool = false;
        private bool _isExecutingCustomTool = false;

        private async Task ProcessLLMDeltaTextAsync(string deltaText)
        {
            if (string.IsNullOrEmpty(deltaText)) return;
            _responseBuffer.Append(deltaText);

            if (!_isResponding && !_isExecutingSystemTool && !_isExecutingCustomTool)
            {
                var fullText = _responseBuffer.ToString();

                // Look for response_to_customer: or other command prefixes
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
            
            if (_isResponding)
            {
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
        }

        private async Task HandleLLMResponseCompletedAsync()
        {
            try
            {
                // Handle any remaining text in the response section
                if (_isResponding && _currentResponseBufferRead < _responseBuffer.Length)
                {
                    var remainingText = _responseBuffer.ToString().Substring(_currentResponseBufferRead);
                    await SynthesizeSpeechAsync(remainingText);
                    TextGenerated?.Invoke(this, new ConversationTextGeneratedEventArgs(remainingText, _currentClientId));
                }         
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

        private async Task<TimeSpan?> SynthesizeSpeechAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            try
            {
                var combinedCancellatinToken = CancellationTokenSource.CreateLinkedTokenSource(_conversationCTS.Token, _currentTTSProcessingTaskCTS.Token).Token;

                TTSTask = _ttsService.SynthesizeTextAsync(text, combinedCancellatinToken);
                var (audioData, audioDuration) = await TTSTask;
                AudioGenerated?.Invoke(this, new ConversationAudioGeneratedEventArgs(audioData, _currentClientId));
                return audioDuration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error synthesizing speech for text: {Text}", text);
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error synthesizing speech: " + ex.Message, ex));

                return null;
            }
        }

        private async Task HandleLLMSystemToolResponseCompletedAsync()
        {
            var llmResponseFull = _responseBuffer.ToString();

            try
            {
                int startFunctionIndex = llmResponseFull.IndexOf("execute_system_function:");

                int endCallIndex = llmResponseFull.IndexOf("end_call(", startFunctionIndex);
                if (endCallIndex != -1)
                {
                    // parse end_call("reason for ending", "message to speak")
                    string reasonForEnding;
                    string? messageToSpeak = null;

                    int reasonForEndingStartIndex = endCallIndex + "end_call(".Length;
                    int reasonForEndingEndIndex = llmResponseFull.IndexOf(",", reasonForEndingStartIndex);
                    if (reasonForEndingEndIndex != -1)
                    {
                        reasonForEnding = llmResponseFull.Substring(reasonForEndingStartIndex, reasonForEndingEndIndex - reasonForEndingStartIndex);
                    }
                    else
                    {
                        reasonForEnding = llmResponseFull.Substring(reasonForEndingStartIndex);
                    }

                    _logger.LogInformation("AI Agent {AgentId} received end_call: {ReasonForEnding} and {MessageToSpeak}", _agentId, reasonForEnding, messageToSpeak);

                    if (!string.IsNullOrWhiteSpace(messageToSpeak))
                    {
                        var durationToWait = await SynthesizeSpeechAsync(messageToSpeak);
                        if (durationToWait != null)
                        {
                            await Task.Delay(durationToWait.Value.Milliseconds + 300);
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

                int transferToAgentIndex = llmResponseFull.IndexOf("transfer_to_agent(", startFunctionIndex);
                if (transferToAgentIndex != -1)
                {

                    return;
                }

                int recieveDTMFInputIndex = llmResponseFull.IndexOf("receive_dtmf_input(", startFunctionIndex);
                if (recieveDTMFInputIndex != -1)
                {

                    return;
                }

                await _llmService.ProcessInputAsync($"response_from_system: Invalid system tool response recieved, The system is only expecting end_call or transfer_to_agent or receive_dtmf_input system tool in the format of [execute_system_function: toolName(toolparms)]", _conversationCTS?.Token ?? CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing system function, {Response}", llmResponseFull);
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error executing system function: " + ex.Message, ex));
                await _llmService.ProcessInputAsync($"response_from_system: There was an internal system error when trying to run the system tool...", _conversationCTS?.Token ?? CancellationToken.None);
            }
            finally
            {
                _responseBuffer.Clear();
                _currentResponseBufferRead = 0;
                _isExecutingSystemTool = false;
            }
        }

        private async Task ProcessToolExecutionAsync(string toolCommand)
        {
            try
            {
                _logger.LogInformation("Executing tool: {ToolCommand}", toolCommand);

                Thinking?.Invoke(this, new ConversationAgentThinkingEventArgs($"Executing tool: {toolCommand}"));

                // Parse the tool command
                // Format is expected to be: toolName:param1=value1,param2=value2
                var parts = toolCommand.Split(':', 2);
                if (parts.Length != 2)
                {
                    throw new FormatException("Invalid tool command format");
                }

                var toolName = parts[0].Trim();
                var paramString = parts[1].Trim();

                // Parse parameters
                var parameters = new Dictionary<string, string>();
                foreach (var param in paramString.Split(','))
                {
                    var keyValue = param.Split('=', 2);
                    if (keyValue.Length == 2)
                    {
                        parameters[keyValue[0].Trim()] = keyValue[1].Trim();
                    }
                }

                // Execute the tool
                var result = await _scriptExecutionManager.ExecuteToolAsync(toolName, parameters);

                // Send the result back to the LLM
                await _llmService.ProcessInputAsync($"Tool {toolName} execution result: {result}",
                    _conversationCTS?.Token ?? CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing tool: {ToolCommand}", toolCommand);
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error executing tool: " + ex.Message, ex));

                // Notify LLM about the error
                await _llmService.ProcessInputAsync($"Error executing tool: {ex.Message}",
                    _conversationCTS?.Token ?? CancellationToken.None);
            }
        }

        #endregion

        private async Task CancelOnGoingAgentProcessingTask()
        {
            _isAcceptingSTTAudio = false;
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
    }
}