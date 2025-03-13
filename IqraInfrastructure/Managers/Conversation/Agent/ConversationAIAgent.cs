using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.Conversation;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Script;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text;

namespace IqraInfrastructure.Managers.Conversation
{
    public class AIAgent : IConversationAgent
    {
        private readonly string _agentId;
        private readonly ILogger<AIAgent> _logger;
        private readonly ISTTService _sttService;
        private readonly ITTSService _ttsService;
        private readonly ILLMService _llmService;
        private readonly BusinessManager _businessManager;
        private readonly SystemPromptGenerator _systemPromptGenerator;
        private readonly ScriptExecutionManager _scriptExecutionManager;

        private ConversationAgentConfiguration? _configuration;
        private CancellationTokenSource? _processingCts;
        private bool _isInitialized;
        private bool _isProcessingAudio;
        private readonly BlockingCollection<byte[]> _audioQueue = new();
        private Task? _audioProcessingTask;
        private readonly Dictionary<string, string> _clientContextMap = new();
        private string? _currentClientId;
        private string? _currentLanguageCode;
        private string? _currentSpeakerVoice;
        private long _businessId;
        private string? _businessAgentId;
        private string? _routeId;

        public string AgentId => _agentId;
        public ConversationAgentType AgentType => ConversationAgentType.AI;

        public event EventHandler<ConversationAudioGeneratedEventArgs>? AudioGenerated;
        public event EventHandler<ConversationTextGeneratedEventArgs>? TextGenerated;
        public event EventHandler<ConversationAgentThinkingEventArgs>? Thinking;
        public event EventHandler<ConversationAgentErrorEventArgs>? ErrorOccurred;

        public AIAgent(
            string agentId,
            ISTTService sttService,
            ITTSService ttsService,
            ILLMService llmService,
            BusinessManager businessManager,
            SystemPromptGenerator systemPromptGenerator,
            ScriptExecutionManager scriptExecutionManager,
            ILogger<AIAgent> logger)
        {
            _agentId = agentId;
            _sttService = sttService;
            _ttsService = ttsService;
            _llmService = llmService;
            _businessManager = businessManager;
            _systemPromptGenerator = systemPromptGenerator;
            _scriptExecutionManager = scriptExecutionManager;
            _logger = logger;

            // Initialize STT event handlers
            _sttService.TranscriptionResultReceived += OnTranscriptionResultReceived;
            _sttService.OnRecoginizingRecieved += OnRecognizingReceived;

            // Initialize LLM event handlers
            _llmService.MessageStreamed += OnLLMMessageStreamed;
        }

        public async Task InitializeAsync(ConversationAgentConfiguration config, CancellationToken cancellationToken)
        {
            if (_isInitialized)
            {
                _logger.LogWarning("AI Agent {AgentId} is already initialized", _agentId);
                return;
            }

            _configuration = config ?? throw new ArgumentNullException(nameof(config));
            _businessId = config.BusinessId;
            _businessAgentId = config.BusinessAgentId;
            _routeId = config.RouteId;
            _currentLanguageCode = config.LanguageCode;

            try
            {
                _processingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                // Initialize services
                _sttService.Initialize();
                _sttService.StartTranscription();

                _ttsService.Initialize();

                // Set up LLM with system prompt
                await ConfigureLLMWithBusinessDataAsync();

                // Start audio processing task
                _isProcessingAudio = true;
                _audioProcessingTask = Task.Run(ProcessAudioQueueAsync, _processingCts.Token);

                _isInitialized = true;
                _logger.LogInformation("AI Agent {AgentId} initialized with business ID {BusinessId}, route {RouteId}",
                    _agentId, _businessId, _routeId);
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
                // Set current client context
                _currentClientId = clientId;

                // Check for special system commands
                if (text.StartsWith("<") && text.EndsWith(">"))
                {
                    await ProcessSystemCommandAsync(text, cancellationToken);
                    return;
                }

                // Process the text through the LLM
                _logger.LogInformation("AI Agent {AgentId} processing text: {Text}", _agentId, text);

                if (clientId != null)
                {
                    // Store the context for this client if needed
                    _clientContextMap[clientId] = text;
                }

                // Trigger LLM processing
                await _llmService.ProcessInputAsync(text, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Normal cancellation
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
                _logger.LogDebug("AI Agent {AgentId} is already shut down", _agentId);
                return;
            }

            try
            {
                // Stop processing
                _isProcessingAudio = false;
                _processingCts?.Cancel();

                // Wait for tasks to complete
                if (_audioProcessingTask != null)
                {
                    await Task.WhenAny(_audioProcessingTask, Task.Delay(1000));
                }

                // Clean up resources
                _sttService.StopTranscription();
                _sttService.TranscriptionResultReceived -= OnTranscriptionResultReceived;
                _sttService.OnRecoginizingRecieved -= OnRecognizingReceived;
                _llmService.MessageStreamed -= OnLLMMessageStreamed;

                _audioQueue.CompleteAdding();
                _processingCts?.Dispose();

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
                while (_isProcessingAudio && !_processingCts!.Token.IsCancellationRequested)
                {
                    if (_audioQueue.TryTake(out var audioData, 100, _processingCts.Token))
                    {
                        // Process audio with STT service
                        _sttService.WriteTranscriptionAudioData(audioData);
                    }
                }
            }
            catch (OperationCanceledException) when (_processingCts!.Token.IsCancellationRequested)
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
                // Get business agent and route configuration
                var businessApp = await _businessManager.GetUserBusinessAppById(_businessId, "ConfigureLLMWithBusinessDataAsync");
                if (!businessApp.Success || businessApp.Data == null)
                {
                    throw new InvalidOperationException($"Business app not found for ID {_businessId}");
                }

                var agent = businessApp.Data.Agents.FirstOrDefault(a => a.Id == _businessAgentId);
                if (agent == null)
                {
                    throw new InvalidOperationException($"Agent not found with ID {_businessAgentId} in business {_businessId}");
                }

                var route = businessApp.Data.Routings.FirstOrDefault(r => r.Id == _routeId);
                if (route == null)
                {
                    throw new InvalidOperationException($"Route not found with ID {_routeId} in business {_businessId}");
                }

                // Generate system prompt
                var systemPrompt = _systemPromptGenerator.GenerateSystemPrompt(
                    businessApp.Data,
                    agent,
                    route,
                    _currentLanguageCode
                );

                // Get script if specified in route
                if (!string.IsNullOrEmpty(route.Agent.OpeningScriptId))
                {
                    await _scriptExecutionManager.LoadScriptAsync(
                        _businessId,
                        route.Agent.OpeningScriptId,
                        _currentLanguageCode
                    );
                }

                // Configure voice for TTS
                ConfigureVoiceForLanguage();

                // Configure LLM
                _llmService.SetSystemPrompt(systemPrompt);

                // Select LLM model based on configuration
                var llmConfig = agent.Integrations.LLM.GetValueOrDefault(_currentLanguageCode)?.FirstOrDefault();
                if (llmConfig != null && llmConfig.FieldValues.TryGetValue("model", out var modelObj))
                {
                    _llmService.SetModel(modelObj.ToString() ?? "");
                }

                // Get opening message if provided
                var openingMessage = agent.Utterances.GreetingMessage.GetValueOrDefault(_currentLanguageCode);
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

        private void ConfigureVoiceForLanguage()
        {
            if (string.IsNullOrEmpty(_currentLanguageCode)) return;

            // Logic to select appropriate voice for the language
            // This would be based on business configuration
            switch (_currentLanguageCode.ToLower())
            {
                case "en":
                    _currentSpeakerVoice = "en-US-GuyNeural";
                    break;
                case "ar":
                    _currentSpeakerVoice = "ar-SA-HamedNeural";
                    break;
                // Add more language mappings as needed
                default:
                    _currentSpeakerVoice = "en-US-GuyNeural"; // Default voice
                    break;
            }

            _logger.LogInformation("Selected voice {Voice} for language {Language}",
                _currentSpeakerVoice, _currentLanguageCode);
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
            // Intermediate transcription result - we could use this to show "thinking" indicators
            // or to implement voice activity detection for barge-in handling
            //_logger.LogTrace("Recognizing speech...");
        }

        private async void OnTranscriptionResultReceived(object? sender, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            try
            {
                _logger.LogInformation("AI Agent {AgentId} received transcription: {Text}", _agentId, text);

                // Process through LLM
                await ProcessTextAsync(text, _currentClientId, _processingCts?.Token ?? CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling transcription result");
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error handling transcription: " + ex.Message, ex));
            }
        }

        private async void OnLLMMessageStreamed(object? sender, object responseObj)
        {
            // Handle different LLM provider responses
            try
            {
                string? deltaText = null;

                // Extract delta text based on the LLM provider
                if (_llmService.GetProviderType() == InterfaceLLMProviderEnum.AnthropicClaude)
                {
                    var response = (Anthropic.SDK.Messaging.MessageResponse)responseObj;
                    if (response.Delta != null)
                    {
                        deltaText = response.Delta.Text;

                        // Check for end of response
                        if (response.Delta.StopReason == "end_turn")
                        {
                            await HandleLLMResponseCompletedAsync();
                            return;
                        }
                    }
                }
                // Add other LLM providers as needed

                // Process the delta text if any
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
        }

        private StringBuilder _responseBuffer = new StringBuilder();
        private StringBuilder _currentResponseSection = new StringBuilder();
        private string _currentResponseType = "parsing"; // parsing, responding, action, etc.

        private async Task ProcessLLMDeltaTextAsync(string deltaText)
        {
            if (string.IsNullOrEmpty(deltaText)) return;

            // Append to overall response
            _responseBuffer.Append(deltaText);

            // Detect response type if we're in parsing phase
            if (_currentResponseType == "parsing" && _responseBuffer.Length > 5)
            {
                var fullText = _responseBuffer.ToString();

                // Look for response_to_customer: or other command prefixes
                if (fullText.StartsWith("response_to_customer:"))
                {
                    _currentResponseType = "responding";
                    _responseBuffer = new StringBuilder(fullText.Replace("response_to_customer:", ""));
                    _currentResponseSection = new StringBuilder();
                }
                else if (fullText.StartsWith("execute_tool:"))
                {
                    _currentResponseType = "executing_tool";
                    var toolCommand = fullText.Replace("execute_tool:", "");
                    _responseBuffer = new StringBuilder();

                    // Process tool execution in a separate task
                    _ = Task.Run(() => ProcessToolExecutionAsync(toolCommand));
                    return;
                }
                else if (fullText.StartsWith("end_call:"))
                {
                    _currentResponseType = "ending_call";
                    var endReason = fullText.Replace("end_call:", "");
                    _responseBuffer = new StringBuilder();

                    // Process end call in a separate task
                    _ = Task.Run(() => ProcessEndCallAsync(endReason));
                    return;
                }
                // Add other command types as needed
            }

            // If we're responding, process the text through TTS in chunks
            if (_currentResponseType == "responding")
            {
                _currentResponseSection.Append(deltaText);

                // Simple sentence chunking - look for end of sentences to create natural TTS chunks
                var sectionText = _currentResponseSection.ToString();

                // Check if we have a complete sentence or a large enough chunk
                bool isCompleteSentence = sectionText.EndsWith(".") || sectionText.EndsWith("!") || sectionText.EndsWith("?");
                bool isLargeChunk = sectionText.Length > 100;

                if ((isCompleteSentence && sectionText.Length > 20) || isLargeChunk)
                {
                    // Process this section
                    await SynthesizeSpeechAsync(sectionText);

                    // Notify about the generated text
                    TextGenerated?.Invoke(this, new ConversationTextGeneratedEventArgs(sectionText, _currentClientId));

                    // Reset for the next section
                    _currentResponseSection.Clear();
                }
            }
        }

        private async Task HandleLLMResponseCompletedAsync()
        {
            try
            {
                // Handle any remaining text in the response section
                if (_currentResponseType == "responding" && _currentResponseSection.Length > 0)
                {
                    var remainingText = _currentResponseSection.ToString();

                    // Synthesize the remaining text
                    await SynthesizeSpeechAsync(remainingText);

                    // Notify about the generated text
                    TextGenerated?.Invoke(this, new ConversationTextGeneratedEventArgs(remainingText, _currentClientId));
                }

                // Reset for the next response
                _responseBuffer.Clear();
                _currentResponseSection.Clear();
                _currentResponseType = "parsing";

                _logger.LogInformation("AI Agent {AgentId} completed response generation", _agentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling LLM response completion");
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error completing response: " + ex.Message, ex));
            }
        }

        private async Task SynthesizeSpeechAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            try
            {
                _logger.LogDebug("Synthesizing speech for text: {Text}", text);

                // Generate audio using TTS
                var audioData = await _ttsService.SynthesizeTextAsync(text, _processingCts?.Token ?? CancellationToken.None);

                // Notify about the generated audio
                AudioGenerated?.Invoke(this, new ConversationAudioGeneratedEventArgs(audioData, _currentClientId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error synthesizing speech for text: {Text}", text);
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error synthesizing speech: " + ex.Message, ex));
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
                    _processingCts?.Token ?? CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing tool: {ToolCommand}", toolCommand);
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error executing tool: " + ex.Message, ex));

                // Notify LLM about the error
                await _llmService.ProcessInputAsync($"Error executing tool: {ex.Message}",
                    _processingCts?.Token ?? CancellationToken.None);
            }
        }

        private async Task ProcessEndCallAsync(string reason)
        {
            try
            {
                _logger.LogInformation("AI Agent {AgentId} requesting to end call: {Reason}", _agentId, reason);

                // Synthesize a goodbye message
                var goodbyeMessage = "Thank you for calling. " + reason;
                await SynthesizeSpeechAsync(goodbyeMessage);

                // Notify about the end call
                TextGenerated?.Invoke(this, new ConversationTextGeneratedEventArgs($"END_CALL: {reason}", _currentClientId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing end call request");
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error ending call: " + ex.Message, ex));
            }
        }

        #endregion
    }
}