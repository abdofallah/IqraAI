using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using IqraCore.Interfaces.AI;

namespace IqraInfrastructure.Services
{
    public class ClaudeStreamingLLMService : IAIService
    {
        private readonly AnthropicClient _client;
        private readonly string _apiKey;

        private int _maxTokens;
        private string _model;
        private decimal _temperature;

        private List<Message> _initialMessages;
        private List<Message> _messagesMemory;

        private string _systemPrompt;
        private Dictionary<string, string> _systemPromptVariables;

        public event EventHandler<object> MessageStreamed;

        public ClaudeStreamingLLMService(string apiKey)
        {
            _apiKey = apiKey;
            _client = new AnthropicClient(apiKey);

            _maxTokens = 128;
            _model = AnthropicModels.Claude3Haiku;
            _temperature = 1m;

            _initialMessages = new List<Message>();
            _messagesMemory = new List<Message>();

            _systemPrompt = "You are Iqra. A helpful AI Assitant.";
            _systemPromptVariables = new Dictionary<string, string>();
        }

        public async Task ProcessInputAsync(string input, CancellationToken cancellationToken)
        {
            var finalMessages = _initialMessages
                .Concat(_messagesMemory)
                .Concat(
                    new List<Message> {
                        new Message
                        {
                            Role = RoleType.User,
                            Content = input
                        }
                    }
            ).ToList();

            var parameters = new MessageParameters
            {
                SystemMessage = _systemPrompt,
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

        public void SetTemplateVariables(Dictionary<string, string> systemPromptVariables)
        {
            _systemPromptVariables = systemPromptVariables;
        }

        public Dictionary<string, string> GetSystemPromptVariables()
        {
            return _systemPromptVariables;
        }

        public void SetInitialMessage(string initialMessage)
        {
            _initialMessages = new List<Message>()
            {
                new Message
                {
                    Role = RoleType.User,
                    Content = "call_started"
                },
                new Message
                {
                    Role = RoleType.Assistant,
                    Content = $"response_to_customer: {initialMessage}"
                }
            };
        }

        public void AddUserMessage(string message)
        {
            _messagesMemory.Add(
                new Message
                {
                    Role = RoleType.User,
                    Content = message
                }
            );
        }

        public void AddAssistantMessage(string message)
        {
            _messagesMemory.Add(
                new Message
                {
                    Role = RoleType.Assistant,
                    Content = message
                }
            );
        }
    }
}