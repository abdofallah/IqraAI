using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;

namespace IqraInfrastructure.Managers.LLM.Providers
{
    public class AnthropicClaudeStreamingLLMService : ILLMService
    {
        private readonly AnthropicClient _client;

        private int _maxTokens;
        private string _model;
        private decimal _temperature;

        private List<Message> _initialMessages;
        private List<Message> _messagesMemory;

        private string _systemPrompt;

        public event EventHandler<object> MessageStreamed;

        public AnthropicClaudeStreamingLLMService(string apiKey, string model)
        {
            _client = new AnthropicClient(apiKey);

            _maxTokens = 1024; // todo make dynamic
            _temperature = 1; // todo make dynamic
            _model = model;

            _initialMessages = new List<Message>();
            _messagesMemory = new List<Message>();

            _systemPrompt = "You are Iqra. A helpful AI Assitant.";
        }

        public async Task ProcessInputAsync(string input, CancellationToken cancellationToken)
        {
            var finalMessages = _initialMessages
                .Concat(_messagesMemory)
                .Concat(
                    new List<Message> {
                        new Message(RoleType.User, input)
                    }
            ).ToList();

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
                await foreach (var res in _client.Messages.StreamClaudeMessageAsync(parameters, cancellationToken))
                {
                    MessageStreamed?.Invoke(this, res);
                }
            }
            catch (Exception ex)
            {
                if (!(ex is TaskCanceledException || ex is OperationCanceledException))
                {
                    Console.WriteLine("ProcessInputAsync Cancelled");
                }
                else
                {
                    Console.WriteLine(ex.Message);
                }
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

        public void SetInitialMessage(string initialMessage)
        {
            _initialMessages = new List<Message>()
            {
                new Message(RoleType.User, "call_started"),
                new Message(RoleType.Assistant, $"response_to_customer: {initialMessage}")
            };
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
    }
}