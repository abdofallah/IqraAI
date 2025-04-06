using IqraCore.Entities.Business;
using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Helper.Agent;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;
using IqraCore.Interfaces.Conversation;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Languages;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.LLM.Providers.Helpers;
using IqraInfrastructure.Managers.Script;
using IqraInfrastructure.Managers.STT;
using IqraInfrastructure.Managers.TTS;
using IqraInfrastructure.Repositories.Business;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Conversation
{
    public class ConversationAIAgent : IConversationAgent
    {  
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ConversationAIAgent> _logger;

        private readonly ConversationSessionManager _conversationSessionManager;

        private readonly BusinessManager _businessManager;
        private readonly SystemPromptGenerator _systemPromptGenerator;
        private readonly LanguagesManager _langaugesManager;
        private readonly STTProviderManager _sttProviderManager;
        private readonly TTSProviderManager _ttsProviderManager;
        private readonly LLMProviderManager _llmProviderManager;
        private ScriptExecutionManager _scriptExecutionManager;
        private readonly BusinessAgentAudioRepository _audioRepository;

        private readonly string _agentId;

        private ConversationAgentConfiguration _agentConfiguration;
        private BusinessApp _businessApp;
        private BusinessAppRoute _currentSessionRoute;
        private BusinessAppAgent _businessAppAgent;
        private string _currentLanguageCode;
        private AgentConversationTypeENUM _currentConversationType;

        private BusinessAppIntegration _sttBusinessIntegrationData;
        private ISTTService _sttService;

        private BusinessAppIntegration _ttsBusinessIntegrationData;
        private ITTSService _ttsService;

        private BusinessAppIntegration _llmBusinessIntegrationData;
        private ILLMService _llmService;
        private string _llmBaseSystemPrompt;

        private ILLMService _interruptingLLMService;

        private CancellationTokenSource _conversationCTS;
        private bool _isInitialized;

        private bool _isProcessingAudio;
        private bool _isAcceptingSTTAudio = false;
        private readonly BlockingCollection<byte[]> _audioQueue = new();
        private Task? _audioProcessingTask;
        private readonly Dictionary<string, string> _clientContextMap = new();
        private string? _currentClientId;

        // --- Audio Processing & Buffering Members ---
        private const int SampleRate = 16000; // Hz
        private const int BitsPerSample = 16; // bits
        private const int Channels = 1; // mono
        private const int BytesPerSample = BitsPerSample / 8;
        private const int ChunkDurationMs = 300; // Desired chunk duration in milliseconds
        private const int BytesPerChunk = (SampleRate * BytesPerSample * Channels * ChunkDurationMs) / 1000;

        // Queue for fully synthesized speech segments waiting to be sent chunk by chunk
        private readonly BlockingCollection<SpeechSegment> _speechAudioQueue = new(new ConcurrentQueue<SpeechSegment>());
        private Task? _audioSendingTask; // Task for the sending loop (speech + background)
        private CancellationTokenSource _audioSendingCTS = new(); // CTS specifically for the audio sending loop (interruptible)

        // Background Audio
        private ReadOnlyMemory<byte> _backgroundAudioData = ReadOnlyMemory<byte>.Empty;
        private int _backgroundAudioPosition = 0;
        private bool _isBackgroundMusicEnabled = false;
        private bool _isBackgroundMusicLoaded = false;
        private float _backgroundMusicVolume = 0.3f; // Example volume factor (0.0 to 1.0)

        // State for the current speech segment being sent
        private ReadOnlyMemory<byte> _currentSpeechSegment = ReadOnlyMemory<byte>.Empty;
        private int _currentSpeechPosition = 0;

        // LLM Tasks
        private StringBuilder _responseBuffer = new StringBuilder();
        private int _currentResponseBufferRead = 0;
        private DateTime? _currentResponseDurationSpeakingStarted = null;
        private TimeSpan _currentResponseDuration = TimeSpan.Zero;

        private StringBuilder _interruptResponseBuffer = new StringBuilder();
        private Task? _interruptLLMTask = null;
        private bool _isProcessingInterruption = false;

        private bool _isResponding = false;
        private bool _isExecutingSystemTool = false;
        private bool _isExecutingCustomTool = false;

        private readonly SemaphoreSlim _llmResponseLock = new SemaphoreSlim(1, 1);

        private CancellationTokenSource _currentLLMProcessingTaskCTS = new();
        private Task? LLMTask = null;
        public string AgentId => _agentId;
        public ConversationAgentType AgentType => ConversationAgentType.AI;

        public event EventHandler<ConversationAudioGeneratedEventArgs>? AudioGenerated;
        public event EventHandler<ConversationTextGeneratedEventArgs>? TextGenerated;
        public event EventHandler<ConversationAgentThinkingEventArgs>? Thinking;
        public event EventHandler<ConversationAgentErrorEventArgs>? ErrorOccurred;

        // DTMF Processing
        private event EventHandler<string>? OnDTMFRecieved;
        private bool IsProcessingDTMFAlready = false;

        public ConversationAIAgent(
            ILoggerFactory loggerFactory,
            ConversationSessionManager sessionManager,
            string agentId,
            BusinessManager businessManager,
            SystemPromptGenerator systemPromptGenerator,
            STTProviderManager sttProviderManager,
            TTSProviderManager ttsProviderManager,
            LLMProviderManager llmProviderManager,
            LanguagesManager languagesManager,
            BusinessAgentAudioRepository audioRepository
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
            _langaugesManager = languagesManager;
            _audioRepository = audioRepository;

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
                _currentConversationType = businessRouteData.Agent.ConversationType;

                _businessAppAgent = await _businessManager.GetAgentsManager().GetAgentById(_agentConfiguration.BusinessId, _currentSessionRoute.Agent.SelectedAgentId);
                if (_businessAppAgent == null)
                {
                    _logger.LogError("Business app agent {AgentId} not found", _currentSessionRoute.Agent.SelectedAgentId);
                    ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Business app agent not found"));
                    return;
                }

                _currentLanguageCode = _currentSessionRoute.Language.DefaultLanguageCode;

                await InitalizeTTSForLangauge();
                await InitalizeLLMForLangauge();
                await InitalizeSTTForLangauge();

                // Start background music loading 
                // _ = LoadBackgroundMusicAsync(); TODO

                // Start audio processing task
                _isProcessingAudio = true;
                _audioProcessingTask = Task.Run(ProcessAudioQueueAsync, _conversationCTS.Token);

                // Start audio sending task
                _audioSendingTask = Task.Run(ProcessAudioSpeakingQueueAsync, _conversationCTS.Token);

                _isInitialized = true;
                _logger.LogInformation("AI Agent {AgentId} initialized with business ID {BusinessId}, route {RouteId}", _agentId, _agentConfiguration.BusinessId, _agentConfiguration.RouteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing AI Agent {AgentId}", _agentId);
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error initializing agent: " + ex.Message, ex));
                await ShutdownAsync("Initialization failed");
                throw;
            }
        }
        private async Task InitalizeLLMForLangauge()
        {
            var defaultLLMService = _businessAppAgent.Integrations.LLM[_currentLanguageCode][0];

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

            if (_currentConversationType == AgentConversationTypeENUM.InterruptibleViaAI)
            {
                FunctionReturnResult<ILLMService?> interuptibleLLMServiceResult;
                if (_currentSessionRoute.Agent.UseCurrentAgentLLMForInterrupting == true)
                {
                    interuptibleLLMServiceResult = await _llmProviderManager.BuildProviderServiceByIntegration(_llmBusinessIntegrationData, defaultLLMService, new Dictionary<string, string> { });         
                }
                else
                {
                    // todo use integration data to build it
                    throw new NotImplementedException("TODO");
                }

                if (!interuptibleLLMServiceResult.Success || interuptibleLLMServiceResult.Data == null)
                {
                    _logger.LogError("Failed to build interuptible LLM service for agent {AgentId} with error: {ErrorMessage}", _agentId, interuptibleLLMServiceResult.Message);
                    ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Failed to build interuptible LLM service for agent with error: " + interuptibleLLMServiceResult.Message));
                    return;
                }

                _interruptingLLMService = interuptibleLLMServiceResult.Data;
                _interruptingLLMService.SetSystemPrompt("Here are the guidelines for thinking before a response (for the thoughts of the model)\r\n<ThinkingGuidelines>\r\n\t- Only use thinking when required else give response directly.\r\n\t- The thinking text should always be summarized and not exceed more than 1 to 3 sentences.\r\n\t- The thinking text should always be less than maximum of 100 characters.\r\n</ThinkingGuidelines>\r\n\r\nYou will be given response of a customer support agent and the current spoken words/sentence by the customer. Decide whether the customer support agent should keep speaking their current sentence or be inntrupted by the customer to let the customer speak.\r\n\r\nIf we should let the customer speak, respond back with: \"allow_interrupt\".\r\nIf we should let the customer support agent speak, respond back with: \"continue_speaking\".");
            }

            // Warmup LLM
            _llmService.AddUserMessage("response_from_system: Call has started.");
            _llmService.SetSystemPrompt("RESPOND WITH ```execute_system_function: acknowledge(\"Call Start\")``` if call has started.");
            await _llmService.ProcessInputAsync(CancellationToken.None);
            _llmService.AddAssistantMessage("execute_system_function: acknowledge(\"Call Start\")");

            var systemPromptResult = await _systemPromptGenerator.GenerateInitialSystemPrompt(
                _businessApp,
                _businessAppAgent,
                _currentSessionRoute,
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
            _llmBaseSystemPrompt = systemPromptResult.Data;

            // Configure LLM
            _llmService.SetSystemPrompt(_llmBaseSystemPrompt);

            // Get script from route
            _scriptExecutionManager = new ScriptExecutionManager(
                _loggerFactory.CreateLogger<ScriptExecutionManager>()
            );
            await _scriptExecutionManager.LoadScriptAsync(
                _businessApp,
                _currentSessionRoute,
                _currentLanguageCode
            );

            // Set Handlers
            _llmService.MessageStreamed += OnLLMMessageStreamed;
        }
        private async Task InitalizeTTSForLangauge()
        {
            var defaultTTSService = _businessAppAgent.Integrations.TTS[_currentLanguageCode][0];

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

            // Init
            _ttsService.Initialize();
        }
        private async Task InitalizeSTTForLangauge()
        {
            var defaultSTTService = _businessAppAgent.Integrations.STT[_currentLanguageCode][0];

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

            _sttService.TranscriptionResultReceived += OnTranscriptionResultReceived;
            _sttService.OnRecoginizingRecieved += OnRecognizingReceived;

            // Initialize services
            _sttService.Initialize();
            _sttService.StartTranscription();
        }
        private async Task LoadBackgroundMusicAsync()
        {
            if (string.IsNullOrWhiteSpace(_businessAppAgent.Settings?.BackgroundAudioUrl))
            {
                _logger.LogInformation("Agent {AgentId}: No background audio URL configured.", _agentId);
                _isBackgroundMusicEnabled = false;
                return;
            }

            string audioUrl = _businessAppAgent.Settings.BackgroundAudioUrl;
            _logger.LogInformation("Agent {AgentId}: Attempting to load background audio from {Url}", _agentId, audioUrl);

            try
            {
                // Assuming the URL is the Minio file ID/path
                bool exists = await _audioRepository.FileExists(audioUrl);
                if (!exists)
                {
                    _logger.LogWarning("Agent {AgentId}: Background audio file not found at {Url}", _agentId, audioUrl);
                    _isBackgroundMusicEnabled = false;
                    return;
                }

                _backgroundAudioData = await _audioRepository.GetFileAsByteArray(audioUrl);

                // Optional: Validate audio format (basic check for non-empty)
                if (_backgroundAudioData.Length == 0 || _backgroundAudioData.Length % BytesPerSample != 0)
                {
                    _logger.LogWarning("Agent {AgentId}: Background audio from {Url} is empty or has invalid length.", _agentId, audioUrl);
                    _backgroundAudioData = ReadOnlyMemory<byte>.Empty;
                    _isBackgroundMusicEnabled = false;
                    return;
                }

                _logger.LogInformation("Agent {AgentId}: Background audio loaded successfully ({Length} bytes).", _agentId, _backgroundAudioData.Length);
                _isBackgroundMusicEnabled = true;
                _isBackgroundMusicLoaded = true;
                _backgroundAudioPosition = 0; // Start from the beginning
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {AgentId}: Error loading background audio from {Url}", _agentId, audioUrl);
                _backgroundAudioData = ReadOnlyMemory<byte>.Empty;
                _isBackgroundMusicEnabled = false;
            }
        }

        // Notifications
        public async Task NotifyConversationStarted()
        {
            string multiLanguageText = "";
            // Check for multi-language before beginning conversation
            if (_currentSessionRoute.Language.MultiLanguageEnabled && _currentSessionRoute.Language.EnabledMultiLanguages.Count > 1)
            {
                for (int i = 0; i < _currentSessionRoute.Language.EnabledMultiLanguages.Count; i++)
                {
                    var language = _currentSessionRoute.Language.EnabledMultiLanguages[i];
                    var languageData = await _langaugesManager.GetLanguageByCode(language.LanguageCode);

                    var languageLocale = language.LanguageCode;
                    if (languageData.Success)
                    {
                        languageLocale = languageData.Data.Name;
                    }

                    string builtMessage = language.MessageToPlay.ToLower().Replace("{number}", Humanizer.NumberToWordsExtension.ToWords((i + 1)).ToString()).Replace("{name}", languageLocale);
                    multiLanguageText += "\n" + builtMessage;
                }

                await SynthesizeAndPlaySpeechAsync(multiLanguageText, _conversationCTS.Token);

                OnDTMFRecieved += async (object? sender, string digit) =>
                {
                    if (IsProcessingDTMFAlready) return;

                    IsProcessingDTMFAlready = true;
                    if (int.TryParse(digit, out int languageIndex))
                    {
                        if (languageIndex > 0 && languageIndex <= _currentSessionRoute.Language.EnabledMultiLanguages.Count)
                        {
                            var language = _currentSessionRoute.Language.EnabledMultiLanguages[languageIndex - 1];

                            if (language.LanguageCode != _currentLanguageCode)
                            {
                                _currentLanguageCode = language.LanguageCode;
                                
                                await InitalizeTTSForLangauge();
                                await InitalizeLLMForLangauge();
                                await InitalizeSTTForLangauge();
                            }

                            // todo ask user maybe play some sound or wait a bit before starting convo

                            OnDTMFRecieved = null;

                            await BeginAgentConversation();
                        }
                        else
                        {
                            await SynthesizeAndPlaySpeechAsync($"Invalid language selection. {multiLanguageText}", _conversationCTS.Token);
                        }     
                    }
                    else
                    {
                        await SynthesizeAndPlaySpeechAsync($"Invalid input. {multiLanguageText}", _conversationCTS.Token);
                    }

                    IsProcessingDTMFAlready = false;
                };
            }
            else
            {
                await BeginAgentConversation();
            }
        }

        public async Task NotifyMaxDurationReached()
        {
            await CancelOnGoingAgentProcessingTask();
            _isAcceptingSTTAudio = false;

            _llmService.AddUserMessage($"response_from_system: Perform execute_system_function: end_call(...) right away while notifying the user taht maximum duration of {_currentSessionRoute.Configuration.MaxCallTimeS} seconds has been reached for the call.");
            await _llmService.ProcessInputAsync(_conversationCTS.Token);
        }

        // On Begin Conversation
        private async Task BeginAgentConversation()
        {
            if (_businessAppAgent.Utterances.OpeningType == BusinessAppAgentOpeningType.AgentFirst)
            {
                string openingMessage = _businessAppAgent.Utterances.GreetingMessage[_currentLanguageCode];

                _llmService.AddAssistantMessage(openingMessage);
                await SynthesizeAndPlaySpeechAsync(openingMessage, _conversationCTS.Token);
            }
            else
            {
                _llmService.AddAssistantMessage("execute_system_function: acknowledge(\"Call Start\")");
            }

            _isAcceptingSTTAudio = true;
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
                if (_isExecutingSystemTool || _isExecutingCustomTool)
                {
                    if (_currentConversationType == AgentConversationTypeENUM.TurnByTurn) return;

                    // AI is busy executing the tool so we will ask the user to wait
                    // Add some kind of either ai processing that allows ai to take the text
                    // and tell if we should force cancel the tool execution or ignore user query for now

                    // OR for now let user speak (A moment as I finish this task)
                    return;
                }

                if (_isResponding)
                {
                    if (_currentConversationType == AgentConversationTypeENUM.TurnByTurn) return;
                    if (_currentResponseDurationSpeakingStarted == null || _responseBuffer.Length == 0) return;
                    if (_isProcessingInterruption)
                    {
                        // todo what to do about this text that caused interurption
                        return;
                    }

                    _isProcessingInterruption = true;

                    _interruptingLLMService.ClearMessages();

                    string textSentToTTS = _responseBuffer.ToString(0, Math.Min(_currentResponseBufferRead, _responseBuffer.Length));
                    TimeSpan elapsedTime = DateTime.UtcNow - _currentResponseDurationSpeakingStarted.Value;
                    double proportionSpoken = Math.Clamp(elapsedTime.TotalSeconds / _currentResponseDuration.TotalSeconds, 0.0, 1.0);
                    int spokenLength = (int)(textSentToTTS.Length * proportionSpoken);
                    var currentSpokenResponse = textSentToTTS.Substring(0, spokenLength);

                    _logger.LogInformation("Current Spoken Response: {CurrentSpokenResponse}", currentSpokenResponse);

                    _interruptingLLMService.AddUserMessage($"current agent response: {currentSpokenResponse}\ncurrent overlaping customer response: {text}");
                    _interruptingLLMService.MessageStreamed += async (sender, responseObj) =>
                    {
                        await CheckIfInterruptible(sender, responseObj, currentSpokenResponse, text, clientId, cancellationToken);
                    };
                    _interruptLLMTask = _interruptingLLMService.ProcessInputAsync(_conversationCTS.Token);
                    return;
                }

                var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_currentLLMProcessingTaskCTS.Token, _conversationCTS.Token, cancellationToken).Token;

                _currentClientId = clientId;
                if (clientId != null)
                {
                    _clientContextMap[clientId] = text;
                }

                _llmService.AddUserMessage($"customer_query: {text}");

                var sessionFilledPrompt = await _systemPromptGenerator.FillSessionInformationInPrompt(_llmBaseSystemPrompt, _conversationSessionManager.PrimaryClientIdentifier(), _currentSessionRoute, _businessAppAgent, _currentLanguageCode);
                if (!sessionFilledPrompt.Success)
                {
                    _logger.LogError(sessionFilledPrompt.Message, "Error filling session information in prompt for AI Agent {AgentId}", _agentId);
                    ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs(sessionFilledPrompt.Message, new Exception(sessionFilledPrompt.Message)));

                    _llmService.SetSystemPrompt(_llmBaseSystemPrompt);
                }
                else
                {
                    _llmService.SetSystemPrompt(sessionFilledPrompt.Data);
                }                 

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
        private async Task CheckIfInterruptible(object? sender, object responseObj, string spokenSoFar, string customerOverlapText, string? clientId, CancellationToken cancellationToken)
        {
            // TODO this could be problematic if more than one interruption is called, fix this
            // solution, assign ids to each request/response and check if the interruption is for that one
            if (!_currentResponseDurationSpeakingStarted.HasValue)
            {
                _logger.LogInformation("Interruption result came after the text was spoken, so we are ignoring it");

                // todo cancel the task and stop
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken).Cancel();
                _interruptingLLMService.ClearMessageStreamed();

                if (_interruptLLMTask != null)
                {
                    _interruptLLMTask.Wait();
                    _interruptLLMTask = null;
                }

                _isProcessingInterruption = false;
                _interruptResponseBuffer.Clear();

                return;
            }

            FunctionReturnResult<(string? deltaText, bool isEndOfResponse)?> chunkExtractResult = LLMStreamingChunkDataExtractHelper.GetChunkData(responseObj, _llmService.GetProviderType());
            if (!chunkExtractResult.Success)
            {
                _logger.LogError("Error extracting LLM chunk, {Reason}", chunkExtractResult.Message);
                return;
            }
            (string? deltaText, bool isEndOfResponse) = chunkExtractResult.Data.Value;

            if (!string.IsNullOrEmpty(deltaText))
            {
                _interruptResponseBuffer.Append(deltaText);
            }

            if (isEndOfResponse)
            {
                _interruptingLLMService.ClearMessageStreamed();

                string result = _interruptResponseBuffer.ToString().ToLower();

                _logger.LogInformation("AI Agent {AgentId} interrupted result: {Result}", _agentId, result);

                if (result.Contains("allow_interrupt"))
                {
                    _logger.LogInformation("AI Agent {AgentId} interrupted by customer", _agentId);

                    var completeResponse = _responseBuffer.ToString(); // must be before cancel ongoing agent tasks

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    await CancelOnGoingAgentProcessingTask();
                    stopwatch.Stop();
                    _logger.LogInformation("CancelOnGoingAgentProcessingTask took {Milliseconds} ms {secodns}s", stopwatch.ElapsedMilliseconds, stopwatch.Elapsed.TotalSeconds);
                  
                    int useLength = Math.Min((spokenSoFar.Length - 1), completeResponse.Length);
                    var modifiedResponse = spokenSoFar + "......(interrupted by customer at this point but expected to speak) " + completeResponse.Substring(useLength);
                    _logger.LogInformation("AI Agent {AgentId} interrupted response: {Response}", _agentId, modifiedResponse);
                    _llmService.AddAssistantMessage(modifiedResponse);

                    _isProcessingInterruption = false;
                    _isResponding = false;
                    _responseBuffer.Clear();
                    _currentResponseBufferRead = 0;
                    _interruptResponseBuffer.Clear();

                    await ProcessTextAsync(customerOverlapText, clientId, cancellationToken);
                }
                else
                {
                    _interruptResponseBuffer.Clear();
                }
            }
        }
        public async Task ProcessDTMFAsync(string text, string? clientId, CancellationToken cancellationToken)
        {
            OnDTMFRecieved?.Invoke(this, text);
        }

        // Agent Speak Audio Process
        private async Task ProcessAudioSpeakingQueueAsync()
        {
            try
            {
                while (!_conversationCTS.Token.IsCancellationRequested)
                {
                    byte[]? chunkToSend = null;
                    bool isSpeechChunk = false;

                    if (!_currentSpeechSegment.IsEmpty)
                    {
                        // Calculate remaining bytes in the current speech segment
                        int remainingSpeechBytes = _currentSpeechSegment.Length - _currentSpeechPosition;
                        int speechChunkSize = Math.Min(BytesPerChunk, remainingSpeechBytes);

                        if (speechChunkSize > 0)
                        {
                            // Get the speech chunk
                            var speechChunk = _currentSpeechSegment.Slice(_currentSpeechPosition, speechChunkSize);

                            // Get corresponding background music chunk (if enabled)
                            var backgroundChunk = GetNextBackgroundChunk(speechChunkSize);

                            // Mix
                            chunkToSend = MixAudioChunks(speechChunk, backgroundChunk);

                            // Update position
                            _currentSpeechPosition += speechChunkSize;
                            isSpeechChunk = true;

                            // Check if segment finished
                            if (_currentSpeechPosition >= _currentSpeechSegment.Length)
                            {
                                _logger.LogDebug("Agent {AgentId}: Finished sending speech segment.", _agentId);
                                _currentSpeechSegment = ReadOnlyMemory<byte>.Empty;
                                _currentSpeechPosition = 0;
                                // Signal that the logical response is finished *speaking*? Maybe not needed.
                            }
                        }
                        else
                        {
                            // Should not happen if logic is correct, but reset just in case
                            _currentSpeechSegment = ReadOnlyMemory<byte>.Empty;
                            _currentSpeechPosition = 0;
                        }
                    }

                    // --- If no speech chunk, check for next segment or background music ---
                    if (chunkToSend == null)
                    {
                        // Try to get the next speech segment from the queue *if* no speech is currently playing
                        if (_currentSpeechSegment.IsEmpty && _speechAudioQueue.TryTake(out var nextSegment))
                        {
                            _logger.LogDebug("Agent {AgentId}: Starting new speech segment ({Duration}).", _agentId, nextSegment.Duration);
                            _currentSpeechSegment = nextSegment.AudioData;
                            _currentSpeechPosition = 0;


                            // Immediately process the first chunk of the new segment
                            int speechChunkSize = Math.Min(BytesPerChunk, _currentSpeechSegment.Length);
                            if (speechChunkSize > 0)
                            {
                                var speechChunk = _currentSpeechSegment.Slice(_currentSpeechPosition, speechChunkSize);
                                var backgroundChunk = GetNextBackgroundChunk(speechChunkSize);
                                chunkToSend = MixAudioChunks(speechChunk, backgroundChunk);
                                _currentSpeechPosition += speechChunkSize;
                                isSpeechChunk = true;


                                if (_currentSpeechPosition >= _currentSpeechSegment.Length) // Handle very short segments
                                {
                                    _logger.LogDebug("Agent {AgentId}: Finished sending short speech segment immediately.", _agentId);
                                    _currentSpeechSegment = ReadOnlyMemory<byte>.Empty;
                                    _currentSpeechPosition = 0;
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Agent {AgentId}: Dequeued speech segment has zero length.", _agentId);
                                _currentSpeechSegment = ReadOnlyMemory<byte>.Empty; // Discard empty segment
                            }
                        }
                        else
                        {
                            // No speech playing and queue is empty, play background music only (if enabled)
                            var backgroundChunk = GetNextBackgroundChunk(BytesPerChunk);
                            if (!backgroundChunk.IsEmpty)
                            {
                                chunkToSend = MixAudioChunks(ReadOnlyMemory<byte>.Empty, backgroundChunk); // Mix with silence
                                // _logger.LogTrace("Agent {AgentId}: Sending background-only chunk.", _agentId);
                            }
                        }
                    }


                    // --- Send the chunk (if any) and wait ---
                    if (chunkToSend != null && chunkToSend.Length > 0)
                    {
                        // Check for cancellation one last time before sending/delaying
                        if (_audioSendingCTS.Token.IsCancellationRequested)
                        {
                            _logger.LogInformation("Agent {AgentId}: Audio sending cancelled before sending chunk.", _agentId);
                            _audioSendingCTS = new CancellationTokenSource(); // Reset CTS for next operation
                            continue; // Skip sending and delay, re-evaluate state
                        }


                        try
                        {
                            // ** TODO: Consider if ReadOnlyMemory<byte> can be sent directly **
                            // If AudioGenerated expects byte[], we need ToArray(). This allocates.
                            // If the event handler can take ReadOnlyMemory<byte>, it's more efficient.
                            // Assuming byte[] for now based on original event args.
                            AudioGenerated?.Invoke(this, new ConversationAudioGeneratedEventArgs(chunkToSend.ToArray(), _currentClientId));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Agent {AgentId}: Error invoking AudioGenerated event.", _agentId);
                            // Decide how to handle this - stop sending? Log and continue?
                        }


                        // Delay for the chunk duration
                        await Task.Delay(ChunkDurationMs, _audioSendingCTS.Token);
                    }
                    else
                    {
                        // No audio to send (no speech, no background), wait briefly to avoid busy-looping
                        await Task.Delay(50, _conversationCTS.Token); // Use the main CTS here
                    }
                }
            }
            catch (OperationCanceledException) when (_conversationCTS.IsCancellationRequested || _audioSendingCTS.IsCancellationRequested)
            {
                _logger.LogInformation("Agent {AgentId}: Audio sending task cancelled.", _agentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in audio sending task for AI Agent {AgentId}", _agentId);
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error in audio sending loop: " + ex.Message, ex));
            }
            finally
            {
                _logger.LogInformation("Agent {AgentId}: Audio sending task finished.", _agentId);
            }
        }
        private ReadOnlyMemory<byte> GetNextBackgroundChunk(int desiredChunkSize)
        {
            if (!_isBackgroundMusicEnabled || !_isBackgroundMusicLoaded || _backgroundAudioData.IsEmpty)
            {
                return ReadOnlyMemory<byte>.Empty;
            }


            int remainingBackgroundBytes = _backgroundAudioData.Length - _backgroundAudioPosition;
            int bytesToTake = Math.Min(desiredChunkSize, remainingBackgroundBytes);


            ReadOnlyMemory<byte> chunk;


            if (bytesToTake < desiredChunkSize)
            {
                // Need to loop: take remaining, then take from start
                var remainingChunk = _backgroundAudioData.Slice(_backgroundAudioPosition, bytesToTake);
                _backgroundAudioPosition = 0; // Reset position
                int neededFromStart = desiredChunkSize - bytesToTake;
                neededFromStart = Math.Min(neededFromStart, _backgroundAudioData.Length); // Handle case where total data < chunk size


                if (neededFromStart > 0)
                {
                    var startChunk = _backgroundAudioData.Slice(0, neededFromStart);
                    // Combine remainingChunk and startChunk - requires allocation!
                    var combined = new byte[bytesToTake + neededFromStart];
                    remainingChunk.CopyTo(combined.AsMemory().Slice(0, bytesToTake));
                    startChunk.CopyTo(combined.AsMemory().Slice(bytesToTake, neededFromStart));
                    chunk = combined;
                    _backgroundAudioPosition = neededFromStart; // Update position
                }
                else // Only remainingChunk was needed (and it was less than desired size)
                {
                    chunk = remainingChunk;
                    // Position is already reset
                }
            }
            else
            {
                // Take a simple chunk
                chunk = _backgroundAudioData.Slice(_backgroundAudioPosition, bytesToTake);
                _backgroundAudioPosition += bytesToTake;
            }


            // Ensure position loops correctly if exactly at the end
            if (_backgroundAudioPosition >= _backgroundAudioData.Length)
            {
                _backgroundAudioPosition = 0;
            }


            return chunk;
        }
        private byte[] MixAudioChunks(ReadOnlyMemory<byte> speechChunk, ReadOnlyMemory<byte> backgroundChunk)
        {
            // Determine the length of the output chunk (max of inputs)
            int outputLength = Math.Max(speechChunk.Length, backgroundChunk.Length);
            if (outputLength == 0) return Array.Empty<byte>(); // Nothing to mix

            // Ensure output length is even for 16-bit samples
            if (outputLength % 2 != 0) outputLength++;

            byte[] mixedOutput = new byte[outputLength];
            var mixedSpan = mixedOutput.AsSpan();

            // Get spans for input chunks
            var speechSpan = speechChunk.Span;
            var backgroundSpan = backgroundChunk.Span;

            // Convert spans to short spans for easier mixing (assumes LittleEndian)
            var speechShortSpan = MemoryMarshal.Cast<byte, short>(speechSpan);
            var backgroundShortSpan = MemoryMarshal.Cast<byte, short>(backgroundSpan);
            var mixedShortSpan = MemoryMarshal.Cast<byte, short>(mixedSpan);

            for (int i = 0; i < mixedShortSpan.Length; i++)
            {
                short speechSample = (i < speechShortSpan.Length) ? speechShortSpan[i] : (short)0;
                short backgroundSample = (i < backgroundShortSpan.Length) ? (short)(backgroundShortSpan[i] * _backgroundMusicVolume) : (short)0;


                // Simple averaging mixing (can clip)
                int mixedSample = (speechSample / 2) + (backgroundSample / 2);


                // Additive mixing with clipping (prevents excessive volume reduction)
                // int mixedSample = speechSample + backgroundSample;


                // Clamp the mixed sample to short range to prevent overflow/clipping artifacts
                mixedShortSpan[i] = (short)Math.Clamp(mixedSample, short.MinValue, short.MaxValue);
            }

            return mixedOutput;
        }

        // Inner Agent Processing
        private async Task ProcessAudioQueueAsync()
        {
            try
            {
                foreach (var audioData in _audioQueue.GetConsumingEnumerable(_conversationCTS.Token))
                {
                    if (_isAcceptingSTTAudio && _sttService != null)
                    {
                        try
                        {
                            _sttService.WriteTranscriptionAudioData(audioData);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Agent {AgentId}: Error writing audio data to STT service.", _agentId);
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (_conversationCTS!.Token.IsCancellationRequested)
            {
                // Normal cancellation
            }
            catch (InvalidOperationException)
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
            Console.WriteLine("Recoginization recieved");
            // lower the volume of the agent speaking if it is by clamping
        }
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
        private async void OnLLMMessageStreamed(object? sender, object responseObj)
        {
            await _llmResponseLock.WaitAsync();

            try
            {
                FunctionReturnResult<(string? deltaText, bool isEndOfResponse)?> chunkExtractResult = LLMStreamingChunkDataExtractHelper.GetChunkData(responseObj, _llmService.GetProviderType());
                if (!chunkExtractResult.Success)
                {
                    _logger.LogError("Error extracting LLM chunk, {Reason}", chunkExtractResult.Message);
                    return;
                }
                (string? deltaText, bool isEndOfResponse) = chunkExtractResult.Data.Value;

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
                        _logger.LogError("Agent {AgentId} Unhandled LLM response: {Response}", _agentId, deltaText);
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
                try
                {
                    _llmResponseLock?.Release();
                }
                catch (ObjectDisposedException) {
                    if (_isInitialized == true)
                    {
                        _logger.LogError("Error releasing LLM response lock");
                        ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error releasing LLM response lock: ObjectDisposedException", null));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error releasing LLM response lock");
                    ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error releasing LLM response lock: " + ex.Message, ex));
                }
            }
        } 

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

                if (_currentResponseDurationSpeakingStarted == null)
                {
                    _currentResponseDurationSpeakingStarted = DateTime.UtcNow;
                }

                // Synthesize the text and update the read position precisely
                var (success, speakSpan) = await SynthesizeSpeechAsync(textToSynthesize);
                _currentResponseDuration = _currentResponseDuration.Add(speakSpan);

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
                    var (success, speakSpan) = await SynthesizeSpeechAsync(remainingText);
                    _currentResponseDuration = _currentResponseDuration.Add(speakSpan);

                    TextGenerated?.Invoke(this, new ConversationTextGeneratedEventArgs(remainingText, _currentClientId));
                }

                var expectedSpeakEnd = _currentResponseDurationSpeakingStarted.Value.Add(_currentResponseDuration);
                var milisecondLeftToSpeak = (expectedSpeakEnd - DateTime.UtcNow).TotalMilliseconds;
                if (milisecondLeftToSpeak > 0)
                {
                    await Task.Delay((int)milisecondLeftToSpeak);
                }

                _llmService.AddAssistantMessage(completeText);
            }
            catch (OperationCanceledException)
            {
                // Ignore
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
                _currentResponseDurationSpeakingStarted = null;
                _currentResponseDuration = TimeSpan.Zero;
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
                            await SynthesizeAndPlaySpeechAsync(messageToSpeak, _conversationCTS.Token);
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
        private async Task<(bool Success, TimeSpan Duration)> SynthesizeSpeechAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || _ttsService == null)
            {
                _logger.LogWarning("Agent {AgentId}: Cannot synthesize empty text or TTS service is null.", _agentId);
                return (false, TimeSpan.Zero);
            }

            using var ttsCts = CancellationTokenSource.CreateLinkedTokenSource(_conversationCTS.Token);

            try
            {
                var (audioData, audioDuration) = await _ttsService.SynthesizeTextAsync(text, ttsCts.Token);
                if (audioData == null || audioData.Length == 0 || audioDuration == null || audioDuration.Value <= TimeSpan.Zero)
                {
                    _logger.LogWarning("Agent {AgentId}: TTS service returned null or empty audio/duration for text: \"{Text}\"", _agentId, text.Length > 50 ? text.Substring(0, 50) + "..." : text);
                    return (false, TimeSpan.Zero);
                }

                var segment = new SpeechSegment(audioData, audioDuration.Value);
                _speechAudioQueue.Add(segment, _conversationCTS.Token);

                return (true, segment.Duration);
            }
            catch (OperationCanceledException) when (ttsCts.IsCancellationRequested)
            {
                _logger.LogInformation("Agent {AgentId}: TTS synthesis cancelled for text: \"{Text}\"", _agentId, text.Length > 50 ? text.Substring(0, 50) + "..." : text);
                return (false, TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {AgentId}: Error synthesizing speech for text: {Text}", _agentId, text.Length > 50 ? text.Substring(0, 50) + "..." : text);
                ErrorOccurred?.Invoke(this, new ConversationAgentErrorEventArgs("Error synthesizing speech: " + ex.Message, ex));
                return (false, TimeSpan.Zero);
            }
        }

        private async Task SynthesizeAndPlaySpeechAsync(string text, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(text)) return;


            // 1. Cancel any currently playing speech first
            await CancelCurrentSpeechPlaybackAsync();


            // 2. Synthesize and queue the new speech
            var (success, duration) = await SynthesizeSpeechAsync(text);


            // 3. Wait for the estimated duration if synthesis was successful
            if (success && duration > TimeSpan.Zero)
            {
                _logger.LogDebug("Agent {AgentId}: Waiting for speech playback: {Duration}", _agentId, duration);
                try
                {
                    // Wait for the duration, but allow cancellation
                    await Task.Delay(duration, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Agent {AgentId}: Wait for speech playback cancelled.", _agentId);
                    // If cancelled, ensure any remaining part of *this specific* speech is also cancelled.
                    await CancelCurrentSpeechPlaybackAsync();
                }
            }
            else if (!success)
            {
                _logger.LogError("Agent {AgentId}: Failed to synthesize speech for critical message: {Text}", _agentId, text);
                // Handle error - maybe log, maybe try fallback?
            }
        }

        // Agent Response Cancellation
        private async Task CancelCurrentSpeechPlaybackAsync()
        {
            _logger.LogDebug("Agent {AgentId}: Cancelling current speech playback.", _agentId);

            _audioSendingCTS.Cancel();

            while (_speechAudioQueue.TryTake(out _)) { }

            _currentSpeechSegment = ReadOnlyMemory<byte>.Empty;
            _currentSpeechPosition = 0;

            await Task.Delay(50);
            if (_audioSendingCTS.IsCancellationRequested)
            {
                _audioSendingCTS.Dispose();
                _audioSendingCTS = new CancellationTokenSource();
            }
        }
        private async Task CancelOnGoingAgentProcessingTask()
        {
            _isAcceptingSTTAudio = false; 
            _sttService?.StopTranscription();

            await _ttsService.StopTextSynthesisAsync();

            _audioSendingCTS.Cancel();
            _currentLLMProcessingTaskCTS.Cancel();

            while (_speechAudioQueue.TryTake(out _)) { }
            _currentSpeechSegment = ReadOnlyMemory<byte>.Empty;
            _currentSpeechPosition = 0;
            _responseBuffer.Clear();

            var tasksToWait = new List<Task>();
            if (LLMTask != null && !LLMTask.IsCompleted) tasksToWait.Add(Task.WhenAny(LLMTask, Task.Delay(500)));
            if (_audioSendingTask != null && !_audioSendingTask.IsCompleted) tasksToWait.Add(Task.WhenAny(_audioSendingTask, Task.Delay(500)));

            if (tasksToWait.Any()) await Task.WhenAll(tasksToWait);

            _currentLLMProcessingTaskCTS = new CancellationTokenSource();
            _audioSendingCTS = new CancellationTokenSource();

            _isResponding = false;
            _isExecutingSystemTool = false;
            _isExecutingCustomTool = false;

            _audioQueue.TryTake(out _);

            _isAcceptingSTTAudio = true;
            _sttService?.StartTranscription();

            _audioSendingTask = Task.Run(ProcessAudioSpeakingQueueAsync, _audioSendingCTS.Token);
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
                _isInitialized = false;
                _isProcessingAudio = false;
                _isAcceptingSTTAudio = false;

                try
                {
                    _conversationCTS?.Cancel();
                    _audioSendingCTS?.Cancel();
                    _currentLLMProcessingTaskCTS?.Cancel();
                }
                catch (ObjectDisposedException) {}
                catch (Exception ex) { _logger.LogError(ex, "Agent {AgentId}: Error cancelling tokens during shutdown.", _agentId); }

                // Clean up resources
                try { _sttService?.StopTranscription(); }
                catch (Exception ex) { _logger.LogError(ex, "Agent {AgentId}: Error stopping STT service during shutdown.", _agentId); }

                if (_sttService != null)
                {
                    try
                    {
                        _sttService.TranscriptionResultReceived -= OnTranscriptionResultReceived;
                        _sttService.OnRecoginizingRecieved -= OnRecognizingReceived;
                    }
                    catch (Exception ex) { _logger.LogWarning(ex, "Agent {AgentId}: Exception unsubscribing STT events.", _agentId); }
                }

                if (_llmService != null)
                {
                    try
                    {
                        _llmService.MessageStreamed -= OnLLMMessageStreamed;
                    }
                    catch (Exception ex) { _logger.LogWarning(ex, "Agent {AgentId}: Exception unsubscribing LLM events.", _agentId); }
                }

                if (_interruptingLLMService != null)
                {
                    try
                    {
                        _interruptingLLMService.ClearMessageStreamed();
                    }
                    catch (Exception ex) { _logger.LogWarning(ex, "Agent {AgentId}: Exception unsubscribing Interrupt LLM events.", _agentId); }
                }

                OnDTMFRecieved = null;

                try { _audioQueue?.CompleteAdding(); } catch (Exception ex) { _logger.LogWarning(ex, "Agent {AgentId}: Exception completing input audio queue.", _agentId); }
                try { _speechAudioQueue?.CompleteAdding(); } catch (Exception ex) { _logger.LogWarning(ex, "Agent {AgentId}: Exception completing speech audio queue.", _agentId); }

                // Wait for tasks to complete
                var tasksToWaitFor = new List<Task>();
                if (_audioProcessingTask != null && !_audioProcessingTask.IsCompleted) tasksToWaitFor.Add(_audioProcessingTask);
                if (_audioSendingTask != null && !_audioSendingTask.IsCompleted) tasksToWaitFor.Add(_audioSendingTask);
                if (LLMTask != null && !LLMTask.IsCompleted) tasksToWaitFor.Add(LLMTask);

                if (tasksToWaitFor.Any())
                {
                    try
                    {
                        _logger.LogDebug("Agent {AgentId}: Waiting for background tasks to complete...", _agentId);
                        // Wait for all tasks or a timeout (e.g., 5 seconds)
                        var allTasks = Task.WhenAll(tasksToWaitFor);
                        await Task.WhenAny(allTasks, Task.Delay(TimeSpan.FromSeconds(10))); // Timeout grace period


                        if (!allTasks.IsCompleted)
                        {
                            _logger.LogWarning("Agent {AgentId}: Not all background tasks completed within the shutdown timeout.", _agentId);
                            // Log which tasks didn't complete (optional)
                            foreach (var task in tasksToWaitFor)
                            {
                                if (!task.IsCompleted) _logger.LogWarning("Agent {AgentId}: Task {TaskId} ({TaskStatus}) did not complete.", _agentId, task.Id, task.Status);
                            }
                        }
                        else
                        {
                            _logger.LogDebug("Agent {AgentId}: Background tasks completed.", _agentId);
                        }
                    }
                    catch (Exception ex) // Catch potential exceptions from Task.WhenAll if tasks faulted
                    {
                        _logger.LogError(ex, "Agent {AgentId}: Exception occurred while waiting for tasks during shutdown.", _agentId);
                    }
                }
                else
                {
                    _logger.LogDebug("Agent {AgentId}: No active background tasks to wait for.", _agentId);
                }

                try { _conversationCTS?.Dispose(); } catch (Exception ex) { _logger.LogWarning(ex, "Agent {AgentId}: Exception disposing conversation CTS.", _agentId); }
                try { _audioSendingCTS?.Dispose(); } catch (Exception ex) { _logger.LogWarning(ex, "Agent {AgentId}: Exception disposing audio sending CTS.", _agentId); }
                try { _currentLLMProcessingTaskCTS?.Dispose(); } catch (Exception ex) { _logger.LogWarning(ex, "Agent {AgentId}: Exception disposing LLM CTS.", _agentId); }
                _audioQueue?.Dispose();
                _speechAudioQueue?.Dispose();
                _llmResponseLock?.Dispose();

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

    internal readonly struct SpeechSegment
    {
        // Stores the raw PCM audio bytes.
        // ReadOnlyMemory<byte> is used for efficient memory handling without unnecessary copying.
        public ReadOnlyMemory<byte> AudioData { get; }

        // Stores the calculated duration of the audio data.
        public TimeSpan Duration { get; }

        // Constructor to initialize the immutable struct.
        public SpeechSegment(ReadOnlyMemory<byte> audioData, TimeSpan duration)
        {
            AudioData = audioData;
            Duration = duration;
        }
    }
}