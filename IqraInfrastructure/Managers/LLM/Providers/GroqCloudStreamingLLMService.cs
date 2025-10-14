using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.LLM.Providers.GroqCloud;
using IqraCore.Interfaces.AI;    
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IqraInfrastructure.Managers.LLM.Providers
{
    public class GroqCloudStreamingLLMService : ILLMService
    {
        private readonly ILogger<GroqCloudStreamingLLMService> _logger;

        private readonly HttpClient _httpClient;
        private readonly CancellationTokenSource _cts;

        private readonly string _apiKey;

        // Configuration
        private int _maxTokens;
        private string _model;
        private float _temperature;
        private float _topP;

        // Session Data
        private string _systemPrompt;
        private List<GroqCloudMessage> _initialMessages;
        private List<GroqCloudMessage> _messagesMemory;
        private object _messageLock = new();

        public event EventHandler<ConversationAgentEventLLMStreamed>? MessageStreamed;
        public void ClearMessageStreamed() => MessageStreamed = null;

        public event EventHandler<ConversationAgentEventLLMStreamCancelled> MessageStreamedCancelled;

        private JsonSerializerOptions requestJsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // Consider using IHttpClientFactory in a real application
        public GroqCloudStreamingLLMService(ILogger<GroqCloudStreamingLLMService> logger, string apiKey, string model)
        {
            _logger = logger;
            _cts = new();

            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _model = model ?? throw new ArgumentNullException(nameof(model));

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            _maxTokens = 1024; // Default, make dynamic
            _temperature = 0.7f; // Default, make dynamic (use float)
            _topP = 1.0f; // Default, make dynamic

            _initialMessages = new List<GroqCloudMessage>();
            _messagesMemory = new List<GroqCloudMessage>();

            _systemPrompt = "You are Iqra. A helpful AI Assistant."; // Default
        }

        public async Task ProcessInputAsync(CancellationToken cancellationToken, string? beforeMessageContext = null, string? afterMessageContext = null)
        {
            var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken).Token;

            var finalMessages = new List<GroqCloudMessage>();
            if (!string.IsNullOrWhiteSpace(_systemPrompt))
            {
                finalMessages.Add(new GroqCloudMessage("system", _systemPrompt));
            }
            finalMessages.AddRange(_initialMessages);
            finalMessages.AddRange(_messagesMemory);

            if (!string.IsNullOrEmpty(beforeMessageContext) && !string.IsNullOrEmpty(afterMessageContext))
            {
                var lastMessage = finalMessages.LastOrDefault();
                if (lastMessage != null && lastMessage.Role == "user")
                {
                    var newText = "";

                    var textData = lastMessage.Content;
                    if (!string.IsNullOrEmpty(beforeMessageContext))
                    {
                        newText = beforeMessageContext + "\n\n" + textData;
                    }
                    if (!string.IsNullOrEmpty(afterMessageContext))
                    {
                        newText = newText + "\n\n" + afterMessageContext;
                    }

                    var newUserMessage = new GroqCloudMessage("user", newText);
                    finalMessages.RemoveAt(finalMessages.Count - 1);
                    finalMessages.Add(newUserMessage);
                }
            }

            if (!finalMessages.Any(m => m.Role == "user"))
            {
                // Handle case where there are no user messages if necessary
                // For now, assume AddUserMessage was called before ProcessInputAsync
                _logger.LogWarning("No user messages found for Groq request.");
                return; // Or throw an exception
            }

            var requestPayload = new GroqCloudRequest(
                Model: _model,
                Messages: finalMessages,
                Temperature: _temperature,
                MaxCompletionTokens: _maxTokens,
                MaxTokens: _maxTokens, // TODO
                TopP: _topP,
                Stream: true
            );

            var jsonPayload = JsonSerializer.Serialize(requestPayload, requestJsonOptions);
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };

            try
            {
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, combinedCancellationToken);
                response.EnsureSuccessStatusCode();

                using var responseStream = await response.Content.ReadAsStreamAsync(combinedCancellationToken);
                using var reader = new StreamReader(responseStream);

                while (!reader.EndOfStream && !combinedCancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync() ?? string.Empty;

                    if (line.StartsWith("data: "))
                    {
                        var dataJson = line.Substring("data: ".Length).Trim();

                        if (dataJson.Equals("[DONE]", StringComparison.OrdinalIgnoreCase))
                        {
                            break; // End of stream signal
                        }

                        try
                        {
                            var chunk = JsonSerializer.Deserialize<GroqCloudStreamChunk>(dataJson);
                            if (chunk != null)
                            {
                                MessageStreamed?.Invoke(this, new ConversationAgentEventLLMStreamed(chunk)); // Pass the entire chunk object
                            }
                        }
                        catch (JsonException jsonEx)
                        {
                            _logger.LogError($"Error deserializing Groq stream chunk: {jsonEx.Message}. JSON: {dataJson}");
                            // Decide how to handle malformed chunks (e.g., log, skip, raise error event)
                        }
                    }

                    // Ignore empty lines or lines not starting with "data: " (like comments ':') / TODO CHECK IF NEEDED
                }
            }
            catch (OperationCanceledException) when (combinedCancellationToken.IsCancellationRequested)
            {
                MessageStreamedCancelled?.Invoke(this, new ConversationAgentEventLLMStreamCancelled()
                {
                    Type = ConversationAgentEventLLMStreamCancelledTypeEnum.OperationCancelled
                });
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError($"Error calling Groq API: {httpEx.Message} (StatusCode: {httpEx.StatusCode})");
                MessageStreamedCancelled?.Invoke(this, new ConversationAgentEventLLMStreamCancelled()
                {
                    Type = ConversationAgentEventLLMStreamCancelledTypeEnum.HttpRequestNotSuccess,
                    ResponseCode = httpEx.StatusCode,
                    ResponseMessage = httpEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error processing Groq stream: {ex.Message}");
                MessageStreamedCancelled?.Invoke(this, new ConversationAgentEventLLMStreamCancelled()
                {
                    Type = ConversationAgentEventLLMStreamCancelledTypeEnum.InternalExceptionError,
                    ResponseMessage = ex.Message
                });
            }
        }

        public void SetModel(string model)
        {
            _model = model;
        }

        public void SetTemperature(decimal temperature)
        {
            // Groq API uses float/number between 0 and 2. Clamp and convert.
            _temperature = (float)Math.Max(0.0m, Math.Min(2.0m, temperature));
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
            lock (_messageLock)
            {
                if (_messagesMemory.Count > 0)
                {
                    var lastMessage = _messagesMemory[_messagesMemory.Count - 1];

                    if (lastMessage.Role == "user")
                    {
                        _messagesMemory[_messagesMemory.Count - 1] = new GroqCloudMessage("user", (lastMessage.Content + "\n" + message));
                        return;
                    }
                }

                _messagesMemory.Add(new GroqCloudMessage("user", message));
            }
        }

        public void AddAssistantMessage(string message)
        {
            lock (_messageLock)
            {
                if (_messagesMemory.Count > 0)
                {
                    var lastMessage = _messagesMemory[_messagesMemory.Count - 1];

                    if (lastMessage.Role == "assistant")
                    {
                        _messagesMemory[_messagesMemory.Count - 1] = new GroqCloudMessage("assistant", (lastMessage.Content + "\n" + message));
                        return;
                    }
                }

                _messagesMemory.Add(new GroqCloudMessage("assistant", message));
            }
        }

        public void EditMessage(int index, string message)
        {
            lock (_messageLock)
            {
                if (index >= 0 && index < _messagesMemory.Count)
                {
                    var existing = _messagesMemory[index];
                    _messagesMemory[index] = new GroqCloudMessage(existing.Role, message);
                }
                else
                {
                    // todo add logger
                    Console.WriteLine($"Warning: EditMessage index {index} out of bounds for Groq message memory.");
                }
            }
        }

        public void ClearMessages()
        {
            lock (_messageLock)
            {
                _messagesMemory.Clear();
            }
        }

        public string GetModel()
        {
            return _model;
        }

        public string GetProviderFullName()
        {
            return "GroqCloud";
        }

        public InterfaceLLMProviderEnum GetProviderType()
        {
            return GetProviderTypeStatic();
        }

        public static InterfaceLLMProviderEnum GetProviderTypeStatic()
        {
            return InterfaceLLMProviderEnum.GroqCloud;
        }

        public void Dispose()
        {
            ClearMessageStreamed();

            _cts?.Cancel();

            // todo check if task ProcessInputAsync ended

            _httpClient.Dispose();

            _cts?.Dispose();
        }
    }
}