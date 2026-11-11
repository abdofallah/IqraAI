using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.ClientModel.Primitives;

namespace IqraInfrastructure.Managers.LLM.Providers
{
    public class OpenAIChatCompletionsCompatibleConfig
    {
        public string Model { get; set; } = "";
        public float? Temperature { get; set; }
        public float? TopP { get; set; }
        public int? MaxTokens { get; set; }
        public float? FrequencyPenalty { get; set; }
        public float? PresencePenalty { get; set; }

        // Thinking / Reasoning
        public bool IncludesThinking { get; set; } = false;
        public string? ThinkingTokenFormat { get; set; } // "<think>", "<thinking>", or custom string
        public bool IncludeThinkingInResponse { get; set; } = false;
    }

    public class OpenAIChatCompletionsCompatibleStreamingLLMService : ILLMService
    {
        private readonly ILogger<OpenAIChatCompletionsCompatibleStreamingLLMService> _logger;
        private readonly ChatClient _client;
        private readonly CancellationTokenSource _cts;

        // Config
        private OpenAIChatCompletionsCompatibleConfig _config;

        // Session Data
        private string _systemPrompt;
        private List<ChatMessage> _messagesMemory;

        // Thinking state tracking
        private bool _isInsideThinkingBlock = false;
        private string _thinkingOpenTag = "";
        private string _thinkingCloseTag = "";

        public event EventHandler<ConversationAgentEventLLMStreamed>? MessageStreamed;
        public void ClearMessageStreamed() => MessageStreamed = null;

        public event EventHandler<ConversationAgentEventLLMStreamCancelled> MessageStreamedCancelled;

        public OpenAIChatCompletionsCompatibleStreamingLLMService(
            ILogger<OpenAIChatCompletionsCompatibleStreamingLLMService> logger,
            string apiKey,
            string endpoint,
            OpenAIChatCompletionsCompatibleConfig config)
        {
            _logger = logger;
            _cts = new();
            _config = config ?? throw new ArgumentNullException(nameof(config));

            // Enforce minimum tokens
            if (_config.MaxTokens.HasValue && _config.MaxTokens.Value < 200)
            {
                _config.MaxTokens = 200;
            }

            // Set up thinking tags if thinking is enabled
            if (_config.IncludesThinking && !string.IsNullOrEmpty(_config.ThinkingTokenFormat))
            {
                _thinkingOpenTag = _config.ThinkingTokenFormat;
                // Derive closing tag: <think> → </think>, <thinking> → </thinking>, custom → </custom>
                if (_thinkingOpenTag.StartsWith("<") && _thinkingOpenTag.EndsWith(">"))
                {
                    _thinkingCloseTag = "</" + _thinkingOpenTag.Substring(1);
                }
            }

            // Use the OpenAI SDK with custom endpoint — works with vLLM, Ollama, LiteLLM, etc.
            var clientOptions = new OpenAIClientOptions()
            {
                Endpoint = new Uri(endpoint),
                UserAgentApplicationId = "Iqra.bot",
                NetworkTimeout = TimeSpan.FromSeconds(10),
                RetryPolicy = new ClientRetryPolicy(),
                Transport = new HttpClientPipelineTransport()
            };

            // Some local servers don't need an API key — use a dummy one if empty
            var credential = new ApiKeyCredential(string.IsNullOrEmpty(apiKey) ? "no-key" : apiKey);
            OpenAIClient client = new OpenAIClient(credential, clientOptions);
            _client = client.GetChatClient(_config.Model);

            _messagesMemory = new List<ChatMessage>();
            _systemPrompt = "You are Iqra. A helpful AI Assistant.";
        }

        public async Task ProcessInputAsync(CancellationToken cancellationToken, string? beforeMessageContext = null, string? afterMessageContext = null)
        {
            var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken).Token;

            var finalMessages = (new List<ChatMessage>() { _systemPrompt }).Concat(_messagesMemory).ToList();

            // Context Injection Logic
            if (!string.IsNullOrEmpty(beforeMessageContext) || !string.IsNullOrEmpty(afterMessageContext))
            {
                var lastMessage = finalMessages.LastOrDefault();
                if (lastMessage != null && lastMessage is UserChatMessage userMessageLast)
                {
                    var newText = userMessageLast.Content.FirstOrDefault()?.Text ?? "";

                    if (!string.IsNullOrEmpty(beforeMessageContext))
                    {
                        newText = beforeMessageContext + "\n\n" + newText;
                    }
                    if (!string.IsNullOrEmpty(afterMessageContext))
                    {
                        newText = newText + "\n\n" + afterMessageContext;
                    }

                    var newUserMessage = ChatMessage.CreateUserMessage(newText);
                    finalMessages.RemoveAt(finalMessages.Count - 1);
                    finalMessages.Add(newUserMessage);
                }
            }

            var parameters = new ChatCompletionOptions()
            {
                MaxOutputTokenCount = _config.MaxTokens,
                Temperature = _config.Temperature,
                TopP = _config.TopP,
                FrequencyPenalty = _config.FrequencyPenalty,
                PresencePenalty = _config.PresencePenalty,
            };

            // Reset thinking state for this request
            _isInsideThinkingBlock = false;

            try
            {
                var completionResult = _client.CompleteChatStreamingAsync(finalMessages, parameters, combinedCancellationToken);

                await foreach (var completion in completionResult)
                {
                    // Handle thinking token filtering if needed
                    if (_config.IncludesThinking && !_config.IncludeThinkingInResponse && !string.IsNullOrEmpty(_thinkingOpenTag))
                    {
                        if (ShouldEmitAfterThinkingFilter(completion))
                        {
                            MessageStreamed?.Invoke(this, new ConversationAgentEventLLMStreamed(completion));
                        }
                    }
                    else
                    {
                        MessageStreamed?.Invoke(this, new ConversationAgentEventLLMStreamed(completion));
                    }
                }
            }
            catch (Exception ex)
            {
                if (!(ex is TaskCanceledException || ex is OperationCanceledException))
                {
                    MessageStreamedCancelled?.Invoke(this, new ConversationAgentEventLLMStreamCancelled { Type = ConversationAgentEventLLMStreamCancelledTypeEnum.InternalExceptionError, ResponseMessage = ex.Message });
                    _logger?.LogError(ex, "OpenAI Chat Completions Compatible Stream Error");
                }
                else
                {
                    MessageStreamedCancelled?.Invoke(this, new ConversationAgentEventLLMStreamCancelled { Type = ConversationAgentEventLLMStreamCancelledTypeEnum.OperationCancelled });
                }
            }
        }

        /// <summary>
        /// Filters out thinking tokens from streaming chunks when IncludeThinkingInResponse is false.
        /// Tracks open/close tags across chunks. Returns true if the chunk should be emitted.
        /// </summary>
        private bool ShouldEmitAfterThinkingFilter(StreamingChatCompletionUpdate completion)
        {
            var contentPart = completion.ContentUpdate?.FirstOrDefault();
            var content = contentPart?.Text;
            if (string.IsNullOrEmpty(content)) return true;

            // Check if we encounter the opening tag
            if (!_isInsideThinkingBlock && content.Contains(_thinkingOpenTag))
            {
                _isInsideThinkingBlock = true;
                return false;
            }

            // Check if we encounter the closing tag
            if (_isInsideThinkingBlock && !string.IsNullOrEmpty(_thinkingCloseTag) && content.Contains(_thinkingCloseTag))
            {
                _isInsideThinkingBlock = false;
                return false;
            }

            // If currently inside a thinking block, suppress the chunk
            if (_isInsideThinkingBlock)
            {
                return false;
            }

            return true;
        }

        // Interface methods
        public void SetModel(string model) => _config.Model = model;
        public void SetTemperature(decimal temperature) => _config.Temperature = (float)temperature;
        public void SetMaxTokens(int maxTokens) => _config.MaxTokens = maxTokens;
        public void SetSystemPrompt(string systemPrompt) => _systemPrompt = systemPrompt;

        public void AddUserMessage(string message) => _messagesMemory.Add(ChatMessage.CreateUserMessage(message));
        public void AddAssistantMessage(string message) => _messagesMemory.Add(ChatMessage.CreateAssistantMessage(message));

        public void EditMessage(int index, string message)
        {
            if (index >= 0 && index < _messagesMemory.Count)
            {
                _messagesMemory[index] = ChatMessage.CreateAssistantMessage(message);
            }
        }

        public void ClearMessages() => _messagesMemory.Clear();
        public string GetModel() => _config.Model;
        public string GetProviderFullName() => "OpenAI Chat Completions Compatible";
        public InterfaceLLMProviderEnum GetProviderType() => GetProviderTypeStatic();
        public static InterfaceLLMProviderEnum GetProviderTypeStatic() => InterfaceLLMProviderEnum.OpenAIChatCompletionsCompatible;

        public void Dispose()
        {
            ClearMessageStreamed();
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
