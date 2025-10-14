using Azure;
using Azure.AI.Inference;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;
using Microsoft.Extensions.Logging;


namespace IqraInfrastructure.Managers.LLM.Providers
{
    public class AzureAIInferenceStreamingLLM : ILLMService
    {
        private readonly ILogger<AzureAIInferenceStreamingLLM> _logger;

        private readonly ChatCompletionsClient _client;
        private readonly CancellationTokenSource _cts;

        // Config
        private int _maxTokens;
        private string _model;
        private float _temperature;
        private float _topP;

        // Session Data
        private string _systemPrompt;
        private List<ChatRequestMessage> _initialMessages;
        private List<ChatRequestMessage> _messagesMemory;

        public event EventHandler<ConversationAgentEventLLMStreamed>? MessageStreamed;
        public void ClearMessageStreamed() => MessageStreamed = null;

        public event EventHandler<ConversationAgentEventLLMStreamCancelled> MessageStreamedCancelled;

        public AzureAIInferenceStreamingLLM(string resourceEndpoint, string resounseAPIKey, string modelDeploymentName)
        {
            _logger = null; // todo
            _cts = new();

            _client = new ChatCompletionsClient(new Uri(resourceEndpoint), new AzureKeyCredential(resounseAPIKey));

            _maxTokens = 1024; // todo make dynamic
            _temperature = 1; // todo make dynamic
            _topP = 1; // todo make dynamic

            _model = modelDeploymentName;

            _initialMessages = new List<ChatRequestMessage>();
            _messagesMemory = new List<ChatRequestMessage>();
            _systemPrompt = "You are Iqra. A helpful AI Assitant.";
        }

        public async Task ProcessInputAsync(CancellationToken cancellationToken, string? beforeMessageContext = null, string? afterMessageContext = null)
        {
            var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken).Token;

            var finalMessages = _initialMessages
                .Concat(_messagesMemory)
                .ToList();
            finalMessages.Prepend(new ChatRequestSystemMessage(_systemPrompt));

            if (!string.IsNullOrEmpty(beforeMessageContext) && !string.IsNullOrEmpty(afterMessageContext))
            {
                var lastMessage = finalMessages.LastOrDefault();
                if (lastMessage != null && lastMessage is ChatRequestUserMessage userMessageLast)
                {
                    var newText = "";

                    var textData = userMessageLast.Content;
                    if (!string.IsNullOrEmpty(beforeMessageContext))
                    {
                        newText = beforeMessageContext + "\n\n" + textData;
                    }
                    if (!string.IsNullOrEmpty(afterMessageContext))
                    {
                        newText = newText + "\n\n" + afterMessageContext;
                    }

                    var newUserMessage = new ChatRequestUserMessage(newText);
                    finalMessages.RemoveAt(finalMessages.Count - 1);
                    finalMessages.Add(newUserMessage);
                }
            }
            

            var parameters = new ChatCompletionsOptions()
            {
                Messages = finalMessages,
                Temperature = _temperature,
                Model = _model,
                MaxTokens = _maxTokens,
                NucleusSamplingFactor = _topP,
                ResponseFormat = ChatCompletionsResponseFormat.CreateTextFormat()
            };

            try
            {
                StreamingResponse<StreamingChatCompletionsUpdate> completionResult = await _client.CompleteStreamingAsync(parameters, combinedCancellationToken);

                await foreach (var completion in completionResult)
                {
                    MessageStreamed?.Invoke(this, new ConversationAgentEventLLMStreamed(completion));
                }
            }
            catch (Exception ex)
            {
                if (!(ex is TaskCanceledException || ex is OperationCanceledException))
                {
                    MessageStreamedCancelled?.Invoke(this, null);
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
                new ChatRequestUserMessage(message)
            );
        }

        public void AddAssistantMessage(string message)
        {
            _messagesMemory.Add(
                new ChatRequestAssistantMessage(message)
            );
        }

        public void EditMessage(int index, string message)
        {
            if (index >= 0 && index < _messagesMemory.Count)
            {
                _messagesMemory[index] = new ChatRequestSystemMessage(message);
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
            return "Azure AI Inference";
        }

        public InterfaceLLMProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public InterfaceLLMProviderEnum GetProviderTypeStatic()
        {
            return InterfaceLLMProviderEnum.AzureAIInference;
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
