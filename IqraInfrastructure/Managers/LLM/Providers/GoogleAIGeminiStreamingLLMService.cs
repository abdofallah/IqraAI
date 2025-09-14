using GenerativeAI;
using GenerativeAI.Types;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.LLM.Providers
{
    public class GoogleAIGeminiStreamingLLMService : ILLMService
    {
        private readonly ILogger<GoogleAIGeminiStreamingLLMService> _logger;

        private readonly GoogleAi _client;
        private readonly CancellationTokenSource _cts;

        // Config
        private int _maxTokens;
        private string _modelId;
        private float _temperature;

        // Session Data
        private GenerativeModel _googleModel;

        private Content _systemInstruction;
        private List<Content> _initialMessages;
        private List<Content> _messagesMemory;

        public event EventHandler<ConversationAgentEventLLMStreamed>? MessageStreamed;
        public void ClearMessageStreamed() => MessageStreamed = null;

        public event EventHandler MessageStreamedCancelled;
    
        public GoogleAIGeminiStreamingLLMService(string apiKey, string modelId)
        {
            _logger = null; // todo
            _cts = new();

            _modelId = modelId;

            _maxTokens = 1024; // todo make dynamic
            _temperature = 1; // todo make dyanmic

            _client = new GoogleAi(apiKey);
            _googleModel = _client.CreateGenerativeModel(_modelId);

            _initialMessages = new List<Content>();
            _messagesMemory = new List<Content>();

            SetSystemPrompt("You are Iqra. A helpful AI Assitant.");
        }

        public async Task ProcessInputAsync(CancellationToken cancellationToken, string? beforeMessageContext = null, string? afterMessageContext = null)
        {
            var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken).Token;

            var finalMessages = _initialMessages.Concat(_messagesMemory).ToList();

            if (!string.IsNullOrEmpty(beforeMessageContext) && !string.IsNullOrEmpty(afterMessageContext))
            {
                var lastMessage = finalMessages.LastOrDefault();
                if (lastMessage != null && lastMessage.Role == "user")
                {
                    var lastMessageWithText = lastMessage.Parts.Find(x => !string.IsNullOrEmpty(x.Text));
                    if (lastMessageWithText != null)
                    {
                        var newText = "";

                        var textData = lastMessageWithText.Text;
                        if (!string.IsNullOrEmpty(beforeMessageContext))
                        {
                            newText = beforeMessageContext + "\n\n" + textData;
                        }
                        if (!string.IsNullOrEmpty(afterMessageContext))
                        {
                            newText = newText + "\n\n" + afterMessageContext;
                        }

                        var newUserMessage = MakeContent("user", newText);
                        finalMessages.RemoveAt(finalMessages.Count - 1);
                        finalMessages.Add(newUserMessage);
                    }
                }
            }
            

            var generationConfig = new GenerationConfig
            {
                Temperature = _temperature,
                MaxOutputTokens = _maxTokens,
            };

            var request = new GenerateContentRequest
            {
                Contents = finalMessages,
                GenerationConfig = generationConfig,
                SystemInstruction = _systemInstruction
            };

            try
            {
                await foreach (var response in _googleModel.StreamContentAsync(request, combinedCancellationToken))
                {
                    MessageStreamed?.Invoke(this, new ConversationAgentEventLLMStreamed(response));
                }
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

        public void ClearMessages()
        {
            _messagesMemory.Clear();
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
            return InterfaceLLMProviderEnum.GoogleAIGemini;
        }

        // HELPERS
        private static Part MakeTextPart(string text) => new Part { Text = text };
        private static Content MakeContent(string role, string text) => new Content { Role = role, Parts = { MakeTextPart(text) } };

        public void Dispose()
        {
            ClearMessageStreamed();

            _cts?.Cancel();

            // todo check if task ProcessInputAsync ended

            _cts?.Dispose();
        }
    }
}