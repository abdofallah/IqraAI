using Google.Api.Gax.Grpc;
using Google.Cloud.AIPlatform.V1;
using Grpc.Core;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;

namespace IqraInfrastructure.Managers.LLM.Providers
{
    public class GoogleGeminiStreamingLLMService : ILLMService
    {
        private readonly PredictionServiceClient _client;
        private readonly string _projectId;
        private readonly string _locationId;
        private readonly string _publisher = "google";

        // Config
        private int _maxTokens;
        private string _modelId;
        private float _temperature;
        private float _topP;

        // Session Data
        private Content _systemInstruction;
        private List<Content> _initialMessages;
        private List<Content> _messagesMemory;

        public event EventHandler<object> MessageStreamed;
        public event EventHandler MessageStreamedCancelled;
    
        public GoogleGeminiStreamingLLMService(string projectId, string locationId, string modelId, int maxOutputTokens, float Temperature, float TopP, string apiKey)
        {
            _projectId = projectId;
            _locationId = locationId;
            _modelId = modelId;

            var clientBuilder = new PredictionServiceClientBuilder
            {
                Endpoint = $"{_locationId}-aiplatform.googleapis.com",
                ApiKey = apiKey
            };
            _client = clientBuilder.Build();

            _maxTokens = maxOutputTokens;
            _temperature = Temperature;
            _topP = TopP;

            _initialMessages = new List<Content>();
            _messagesMemory = new List<Content>();

            SetSystemPrompt("You are Iqra. A helpful AI Assitant.");
        }

        public async Task ProcessInputAsync(string input, CancellationToken cancellationToken)
        {
            var finalMessages = _initialMessages.Concat(_messagesMemory).ToList();
            finalMessages.Add(MakeContent("user", input));

            var generationConfig = new GenerationConfig
            {
                Temperature = _temperature,
                MaxOutputTokens = _maxTokens,
                TopP = _topP,
            };

            var request = new GenerateContentRequest
            {
                Model = GetFullModelName(),
                Contents = { finalMessages },
                GenerationConfig = generationConfig,
                SystemInstruction = _systemInstruction
            };

            var callSettings = CallSettings.FromCancellationToken(cancellationToken);

            try
            {
                using var responseStream = _client.StreamGenerateContent(request, callSettings);
                var asyncResponseStream = responseStream.GetResponseStream();
                await foreach (var response in asyncResponseStream.WithCancellation(cancellationToken))
                {
                    MessageStreamed?.Invoke(this, response);
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
            {
                MessageStreamedCancelled?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException ex)
            {
                MessageStreamedCancelled?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing Gemini stream: {ex.Message}");
                // TODO: Replace with proper logging
            }
        }

        public void SetModel(string modelId)
        {
            _modelId = modelId;
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
            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                _systemInstruction = new Content { Parts = { MakeTextPart(systemPrompt) } };
            }
            else
            {
                _systemInstruction = null;
            }
        }

        public void SetInitialMessage(string initialAssistantMessage)
        {
            _initialMessages = new List<Content>()
            {
                 MakeContent("user", "Start of conversation."),
                 MakeContent("model", initialAssistantMessage)
            };

             _messagesMemory.Clear();
        }

         public void AddUserMessage(string message)
        {
            _messagesMemory.Add(MakeContent("user", message));
        }

        public void AddAssistantMessage(string message)
        {
             _messagesMemory.Add(MakeContent("model", message));
        }

        public void EditMessage(int index, string message)
        {
            if (index >= 0 && index < _messagesMemory.Count)
            {
                var existingContent = _messagesMemory[index];
                if (existingContent.Role.Equals("model", StringComparison.OrdinalIgnoreCase))
                {
                    _messagesMemory[index] = MakeContent("model", message);
                }
            }
        }

        public string GetModel()
        {
            return _modelId;
        }

        public string GetProviderFullName()
        {
            return "Google Gemini";
        }

        public InterfaceLLMProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public static InterfaceLLMProviderEnum GetProviderTypeStatic()
        {
            return InterfaceLLMProviderEnum.GoogleGemini;
        }

        // HELPERS
        private string GetFullModelName()
        {
            return $"projects/{_projectId}/locations/{_locationId}/publishers/{_publisher}/models/{_modelId}";
        }
        private static Part MakeTextPart(string text) => new Part { Text = text };
        private static Content MakeContent(string role, string text) => new Content { Role = role, Parts = { MakeTextPart(text) } };
    }
}