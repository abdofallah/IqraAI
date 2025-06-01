using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.ClientModel.Primitives;

namespace IqraInfrastructure.Managers.LLM.Providers
{
    public class OpenAIGPTStreamingLLMService : ILLMService
    {
        private readonly ILogger<OpenAIGPTStreamingLLMService> _logger;

        private readonly ChatClient _client;
        private readonly CancellationTokenSource _cts;

        // Config
        private int _maxTokens;
        private string _model;
        private float _temperature;
        private float _topP;

        // Session Data
        private string _systemPrompt;
        private List<ChatMessage> _initialMessages;
        private List<ChatMessage> _messagesMemory;

        public event EventHandler<object>? MessageStreamed;
        public void ClearMessageStreamed() => MessageStreamed = null;

        public event EventHandler MessageStreamedCancelled;

        public OpenAIGPTStreamingLLMService(string APIKey, string Model, string Endpoint)
        {
            _logger = null; // todo
            _cts = new();

            OpenAIClient client = new OpenAIClient(new ApiKeyCredential(APIKey), new OpenAIClientOptions()
            {
                Endpoint = new Uri(Endpoint),
                UserAgentApplicationId = "Iqra.bot",
                NetworkTimeout = TimeSpan.FromSeconds(10),
                RetryPolicy = new ClientRetryPolicy(),
                Transport = new HttpClientPipelineTransport()
            });

            _client = client.GetChatClient(Model);

            _maxTokens = 1024; // todo make dynamic
            _temperature = 1; // todo make dynamic
            _topP = 1; // todo make dynamic

            _model = Model;

            _initialMessages = new List<ChatMessage>();
            _messagesMemory = new List<ChatMessage>();
            _systemPrompt = "You are Iqra. A helpful AI Assitant.";
        }

        public async Task ProcessInputAsync(CancellationToken cancellationToken)
        {
            var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken).Token;

            var finalMessages = (new List<ChatMessage>() { _systemPrompt }).Concat(_messagesMemory).ToList();

            var parameters = new ChatCompletionOptions()
            {
                MaxOutputTokenCount = _maxTokens,
                ResponseFormat = ChatResponseFormat.CreateTextFormat(),
                Temperature = _temperature,
                TopP = _topP
            };

            try
            {
                var completionResult = _client.CompleteChatStreamingAsync(finalMessages, parameters, combinedCancellationToken);

                await foreach (var completion in completionResult)
                {
                    MessageStreamed?.Invoke(this, completion);
                }
            }
            catch (Exception ex)
            {
                if (!(ex is TaskCanceledException || ex is OperationCanceledException))
                {
                    MessageStreamedCancelled?.Invoke(this, EventArgs.Empty);
                    // TODO IMPLEMENT LOGGER
                    Console.WriteLine("ProcessInputAsync Cancelled");
                }
                else
                {
                    // TODO IMPLEMENT LOGGER
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

        public void AddUserMessage(string message)
        {
            _messagesMemory.Add(
                ChatMessage.CreateUserMessage(message)
            );
        }

        public void AddAssistantMessage(string message)
        {
            _messagesMemory.Add(
                ChatMessage.CreateAssistantMessage(message)
            );
        }

        public void EditMessage(int index, string message)
        {
            if (index >= 0 && index < _messagesMemory.Count)
            {
                _messagesMemory[index] = ChatMessage.CreateAssistantMessage(message);
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
            return "OpenAI GPT";
        }

        public InterfaceLLMProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public static InterfaceLLMProviderEnum GetProviderTypeStatic()
        {
            return InterfaceLLMProviderEnum.OpenAIGPT;
        }

        public void Dispose()
        {
            ClearMessageStreamed();

            _cts?.Cancel();

            // todo check if task ProcessInputAsync ended

            _cts?.Dispose();
        }
    }
}
