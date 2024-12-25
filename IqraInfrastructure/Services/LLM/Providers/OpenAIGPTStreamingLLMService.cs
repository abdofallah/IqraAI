using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;

namespace IqraInfrastructure.Services.LLM.Providers
{
    public class OpenAIGPTStreamingLLMService : IAIService
    {
        private readonly OpenAIService _client;

        // Config
        private int _maxTokens;
        private string _model;
        private float _temperature;
        private float _topP;

        // Session Data
        private string _systemPrompt;
        private List<ChatMessage> _initialMessages;
        private List<ChatMessage> _messagesMemory;
        public event EventHandler<object> MessageStreamed;

        public OpenAIGPTStreamingLLMService(string APIKey, int MaxOutputToken, float Temperature, float TopP, string Model)
        {
            _client = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = APIKey
            });

            _maxTokens = MaxOutputToken;
            _temperature = Temperature;
            _model = Model;
            _topP = TopP;

            _initialMessages = new List<ChatMessage>();
            _messagesMemory = new List<ChatMessage>();
            _systemPrompt = "You are Iqra. A helpful AI Assitant.";
        }

        public async Task ProcessInputAsync(string input, CancellationToken cancellationToken)
        {
            var finalMessages = _initialMessages
                .Concat(_messagesMemory)
                .Concat(
                    new List<ChatMessage> {
                        ChatMessage.FromUser(input)
                    }
            ).ToList();

            var parameters = new CompletionCreateRequest()
            {
                Prompt = _systemPrompt,
                MaxTokens = _maxTokens,
                Temperature = _temperature,
                Stream = true,
                Model = _model,
                TopP = _topP
            };

            try
            {
                var completionResult = _client.Completions.CreateCompletionAsStream(parameters, _model, cancellationToken);

                await foreach (var completion in completionResult)
                {
                    MessageStreamed?.Invoke(this, completion);
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
            _temperature = (float)temperature;
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
            _initialMessages = new List<ChatMessage>()
            {
                ChatMessage.FromUser("call_started"),
                ChatMessage.FromAssistant($"response_to_customer: {initialMessage}")
            };
        }

        public void AddUserMessage(string message)
        {
            _messagesMemory.Add(
                ChatMessage.FromUser(message)
            );
        }

        public void AddAssistantMessage(string message)
        {
            _messagesMemory.Add(
                ChatMessage.FromAssistant(message)
            );
        }

        public string GetProviderFullName()
        {
            return "OpenAI GPT";
        }

        public static InterfaceLLMProviderEnum GetProviderType()
        {
            return InterfaceLLMProviderEnum.OpenAIGPT;
        }  
    }
}
