using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.LLM.Providers
{
    public class AnthropicClaudeStreamingLLMService : ILLMService
    {
        private readonly ILogger<AnthropicClaudeStreamingLLMService> _logger;

        private readonly AnthropicClient _client;
        private readonly CancellationTokenSource _cts;

        private int _maxTokens;
        private string _model;
        private decimal _temperature;

        private List<Message> _initialMessages;
        private List<Message> _messagesMemory;

        private string _systemPrompt;

        public event EventHandler<object>? MessageStreamed;
        public void ClearMessageStreamed() => MessageStreamed = null;

        public event EventHandler MessageStreamedCancelled;

        public AnthropicClaudeStreamingLLMService(string apiKey, string model)
        {
            _logger = null; // todo

            _client = new AnthropicClient(apiKey);
            _cts = new();

            _maxTokens = 1024; // todo make dynamic
            _temperature = 1; // todo make dynamic
            _model = model;

            _initialMessages = new List<Message>();
            _messagesMemory = new List<Message>();

            _systemPrompt = "You are Iqra. A helpful AI Assitant.";
        }

        public async Task ProcessInputAsync(CancellationToken cancellationToken, string? beforeMessageContext = null, string? afterMessageContext = null)
        {
            var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken).Token;

            List<Message> finalMessages = _initialMessages
                .Concat(_messagesMemory)
                .ToList();

            var lastMessage = finalMessages.LastOrDefault();
            if (lastMessage != null && lastMessage.Role == RoleType.User)
            {       
                var messageContent = lastMessage.Content.Find(x => x.Type == ContentType.text);
                if (messageContent != null)
                {
                    var newText = "";

                    var textData = ((TextContent)messageContent).Text;
                    if (!string.IsNullOrEmpty(beforeMessageContext))
                    {
                        newText = beforeMessageContext + "\n\n" + textData;
                    }
                    if (!string.IsNullOrEmpty(afterMessageContext))
                    {
                        newText = newText + "\n\n" + afterMessageContext;
                    }

                    var newUserMessage = new Message(RoleType.User, newText);
                    finalMessages.RemoveAt(finalMessages.Count - 1);
                    finalMessages.Add(newUserMessage);
                }
            }

            var parameters = new MessageParameters
            {
                System = new List<SystemMessage>() { new SystemMessage(_systemPrompt) },
                Messages = finalMessages,
                MaxTokens = _maxTokens,
                Model = _model,
                Stream = true,
                Temperature = _temperature,
            };

            try
            {
                await foreach (var res in _client.Messages.StreamClaudeMessageAsync(parameters, combinedCancellationToken))
                {
                    MessageStreamed?.Invoke(this, res);
                }
            }
            catch (TaskCanceledException ex)
            {
                MessageStreamedCancelled?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException ex)
            {
                MessageStreamedCancelled?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void SetModel(string model)
        {
            _model = model;
        }

        public void SetTemperature(decimal temperature)
        {
            _temperature = temperature;
        }

        public void SetMaxTokens(int maxTokens)
        {
            _maxTokens = maxTokens;
        }

        public void SetSystemPrompt(string systemPrompt)
        {
            _systemPrompt = systemPrompt;
        }

        public void AddUserMessage(string message)
        {
            _messagesMemory.Add(
                new Message(RoleType.User, message)
            );
        }

        public void AddAssistantMessage(string message)
        {
            _messagesMemory.Add(
                new Message(RoleType.Assistant, message)
            );
        }

        public void EditMessage(int index, string message)
        {
            if (index >= 0 && index < _messagesMemory.Count)
            {
                _messagesMemory[index] = new Message(RoleType.Assistant, message);
            }
        }

        public void ClearMessages()
        {
            _messagesMemory.Clear();
        }

        public string GetModel()
        {
            return _model;
        }

        public string GetProviderFullName()
        {
            return "Anthropic Claude";
        }

        public InterfaceLLMProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public static InterfaceLLMProviderEnum GetProviderTypeStatic()
        {
            return InterfaceLLMProviderEnum.AnthropicClaude;
        }

        public void Dispose()
        {
            ClearMessageStreamed();

            _cts?.Cancel();

            // todo check if task ProcessInputAsync ended

            _client.Dispose();

            _cts?.Dispose();
        }
    }
}