using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.LLM.Providers
{
    public class AnthropicClaudeConfig
    {
        public string Model { get; set; } = "";
        public decimal? Temperature { get; set; }
        public decimal? TopP { get; set; }
        public int? TopK { get; set; }
        public int? MaxTokens { get; set; }

        public bool ThinkingEnabled { get; set; }
        public int? ThinkingBudgetTokens { get; set; }

        public string? InferenceGeo { get; set; }

        // Fields for future SDK support (currently placeholder)
        public string? ServiceTier { get; set; } // "auto", "standard_only"
    }

    public class AnthropicClaudeStreamingLLMService : ILLMService
    {
        private readonly ILogger<AnthropicClaudeStreamingLLMService> _logger;
        private readonly AnthropicClient _client;
        private readonly CancellationTokenSource _cts;

        private AnthropicClaudeConfig _config;
        private List<Message> _initialMessages;
        private List<Message> _messagesMemory;
        private string _systemPrompt;

        public event EventHandler<ConversationAgentEventLLMStreamed>? MessageStreamed;
        public void ClearMessageStreamed() => MessageStreamed = null;

        public event EventHandler<ConversationAgentEventLLMStreamCancelled> MessageStreamedCancelled;

        public AnthropicClaudeStreamingLLMService(ILogger<AnthropicClaudeStreamingLLMService> logger, string apiKey, AnthropicClaudeConfig config)
        {
            _logger = logger;
            _config = config ?? throw new ArgumentNullException(nameof(config));

            // Enforce safe defaults if missing
            if (!_config.MaxTokens.HasValue || _config.MaxTokens.Value < 200)
            {
                _config.MaxTokens = 200;
            }

            _client = new AnthropicClient(apiKey);
            _cts = new();

            _initialMessages = new List<Message>();
            _messagesMemory = new List<Message>();
            _systemPrompt = "You are Iqra. A helpful AI Assistant.";
        }

        public async Task ProcessInputAsync(CancellationToken cancellationToken, string? beforeMessageContext = null, string? afterMessageContext = null)
        {
            var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken).Token;

            List<Message> finalMessages = _initialMessages.Concat(_messagesMemory).ToList();

            // Context Injection
            if (!string.IsNullOrEmpty(beforeMessageContext) || !string.IsNullOrEmpty(afterMessageContext))
            {
                var lastMessage = finalMessages.LastOrDefault();
                if (lastMessage != null && lastMessage.Role == RoleType.User)
                {
                    var messageContent = lastMessage.Content.Find(x => x.Type == ContentType.text);
                    if (messageContent != null)
                    {
                        var textData = ((TextContent)messageContent).Text;
                        if (!string.IsNullOrEmpty(beforeMessageContext)) textData = beforeMessageContext + "\n\n" + textData;
                        if (!string.IsNullOrEmpty(afterMessageContext)) textData = textData + "\n\n" + afterMessageContext;

                        finalMessages.RemoveAt(finalMessages.Count - 1);
                        finalMessages.Add(new Message(RoleType.User, textData));
                    }
                }
            }

            var parameters = new MessageParameters
            {
                System = new List<SystemMessage>() { new SystemMessage(_systemPrompt) },
                Messages = finalMessages,
                Model = _config.Model,
                Stream = true,
                MaxTokens = _config.MaxTokens!.Value,
                Temperature = _config.Temperature,
                TopP = _config.TopP,
                TopK = _config.TopK
            };

            // Logic for Thinking (Extended Thinking)
            if (_config.ThinkingEnabled && _config.ThinkingBudgetTokens.HasValue && _config.ThinkingBudgetTokens.Value >= 1024)
            {
                parameters.Thinking = new ThinkingParameters
                {
                    BudgetTokens = _config.ThinkingBudgetTokens.Value
                };

                parameters.Temperature = 1.0m;
            }

            try
            {
                await foreach (var res in _client.Messages.StreamClaudeMessageAsync(parameters, combinedCancellationToken))
                {
                    MessageStreamed?.Invoke(this, new ConversationAgentEventLLMStreamed(res));
                }
            }
            catch (TaskCanceledException)
            {
                MessageStreamedCancelled?.Invoke(this, new ConversationAgentEventLLMStreamCancelled { Type = ConversationAgentEventLLMStreamCancelledTypeEnum.OperationCancelled });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Anthropic API Error");
                MessageStreamedCancelled?.Invoke(this, new ConversationAgentEventLLMStreamCancelled { Type = ConversationAgentEventLLMStreamCancelledTypeEnum.InternalExceptionError, ResponseMessage = ex.Message });
            }
        }

        public void SetModel(string model) => _config.Model = model;
        public void SetTemperature(decimal temperature) => _config.Temperature = temperature;
        public void SetMaxTokens(int maxTokens) => _config.MaxTokens = maxTokens;
        public void SetSystemPrompt(string systemPrompt) => _systemPrompt = systemPrompt;

        public void AddUserMessage(string message) => _messagesMemory.Add(new Message(RoleType.User, message));
        public void AddAssistantMessage(string message) => _messagesMemory.Add(new Message(RoleType.Assistant, message));

        public void EditMessage(int index, string message)
        {
            if (index >= 0 && index < _messagesMemory.Count)
                _messagesMemory[index] = new Message(_messagesMemory[index].Role, message);
        }

        public void ClearMessages() => _messagesMemory.Clear();
        public string GetModel() => _config.Model;
        public string GetProviderFullName() => "Anthropic Claude";
        public InterfaceLLMProviderEnum GetProviderType() => GetProviderTypeStatic();
        public static InterfaceLLMProviderEnum GetProviderTypeStatic() => InterfaceLLMProviderEnum.AnthropicClaude;

        public void Dispose()
        {
            ClearMessageStreamed();
            _cts?.Cancel();
            _client.Dispose();
            _cts?.Dispose();
        }
    }
}