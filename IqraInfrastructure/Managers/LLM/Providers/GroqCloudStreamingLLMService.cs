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
    public class GroqCloudConfig
    {
        public string Model { get; set; } = "";
        public float? Temperature { get; set; }
        public float? TopP { get; set; }
        public int? MaxCompletionTokens { get; set; }
        public int? Seed { get; set; }
        public string? ServiceTier { get; set; }
        public bool IncludeReasoning { get; set; }
        public string? ReasoningFormat { get; set; }
        public string? ReasoningEffort { get; set; }
    }

    public class GroqCloudStreamingLLMService : ILLMService
    {
        private readonly ILogger<GroqCloudStreamingLLMService> _logger;
        private readonly HttpClient _httpClient;
        private readonly CancellationTokenSource _cts;
        private readonly string _apiKey;

        // Config
        private GroqCloudConfig _config;

        // Session Data
        private string _systemPrompt;
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

        public GroqCloudStreamingLLMService(ILogger<GroqCloudStreamingLLMService> logger, string apiKey, GroqCloudConfig config)
        {
            _logger = logger;
            _cts = new();
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            // Validate minimum tokens safeguard
            if (_config.MaxCompletionTokens.HasValue && _config.MaxCompletionTokens.Value < 200)
            {
                _config.MaxCompletionTokens = 200; // Enforce platform minimum
            }

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            _messagesMemory = new List<GroqCloudMessage>();
            _systemPrompt = "You are Iqra. A helpful AI Assistant.";
        }

        public async Task ProcessInputAsync(CancellationToken cancellationToken, string? beforeMessageContext = null, string? afterMessageContext = null)
        {
            var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken).Token;

            var finalMessages = new List<GroqCloudMessage>();
            if (!string.IsNullOrWhiteSpace(_systemPrompt))
            {
                finalMessages.Add(new GroqCloudMessage("system", _systemPrompt));
            }
            finalMessages.AddRange(_messagesMemory);

            // Context Injection Logic
            if (!string.IsNullOrEmpty(beforeMessageContext) || !string.IsNullOrEmpty(afterMessageContext))
            {
                var lastMessage = finalMessages.LastOrDefault();
                if (lastMessage != null && lastMessage.Role == "user")
                {
                    var newText = lastMessage.Content;
                    if (!string.IsNullOrEmpty(beforeMessageContext)) newText = beforeMessageContext + "\n\n" + newText;
                    if (!string.IsNullOrEmpty(afterMessageContext)) newText = newText + "\n\n" + afterMessageContext;

                    finalMessages.RemoveAt(finalMessages.Count - 1);
                    finalMessages.Add(new GroqCloudMessage("user", newText));
                }
            }

            if (!finalMessages.Any(m => m.Role == "user"))
            {
                _logger.LogWarning("No user messages found for Groq request.");
                return;
            }

            var requestPayload = new GroqCloudRequest()
            {
                Model = _config.Model,
                Messages = finalMessages,
                Temperature = _config.Temperature,
                TopP = _config.TopP,
                MaxCompletionTokens = _config.MaxCompletionTokens,
                Seed = _config.Seed,
                ServiceTier = _config.ServiceTier,        
                Stream = true
            };

            if (_config.IncludeReasoning && !string.IsNullOrEmpty(_config.ReasoningFormat) && !string.IsNullOrEmpty(_config.ReasoningEffort))
            {
                requestPayload.IncludeReasoning = _config.IncludeReasoning;
                requestPayload.ReasoningFormat = _config.ReasoningFormat;
                requestPayload.ReasoningEffort = _config.ReasoningEffort;
            }

            var jsonPayload = JsonSerializer.Serialize(requestPayload, requestJsonOptions);
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };

            try
            {
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, combinedCancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Groq API Error: {response.StatusCode} - {errorContent}");
                }

                using var responseStream = await response.Content.ReadAsStreamAsync(combinedCancellationToken);
                using var reader = new StreamReader(responseStream);

                while (!reader.EndOfStream && !combinedCancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync() ?? string.Empty;

                    if (line.StartsWith("data: "))
                    {
                        var dataJson = line.Substring("data: ".Length).Trim();
                        if (dataJson.Equals("[DONE]", StringComparison.OrdinalIgnoreCase)) break;

                        try
                        {
                            var chunk = JsonSerializer.Deserialize<GroqCloudStreamChunk>(dataJson);
                            if (chunk != null)
                            {
                                MessageStreamed?.Invoke(this, new ConversationAgentEventLLMStreamed(chunk));
                            }
                        }
                        catch (JsonException jsonEx)
                        {
                            _logger.LogError($"Error deserializing Groq stream chunk: {jsonEx.Message}");
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (combinedCancellationToken.IsCancellationRequested)
            {
                MessageStreamedCancelled?.Invoke(this, new ConversationAgentEventLLMStreamCancelled() { Type = ConversationAgentEventLLMStreamCancelledTypeEnum.OperationCancelled });
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError($"Error calling Groq API: {httpEx.Message}");
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

        // Interface methods implementation
        public void SetModel(string model) => _config.Model = model;

        // These setters might need updating if interface changes, keeping for compatibility
        public void SetTemperature(decimal temperature) => _config.Temperature = (float)temperature;
        public void SetMaxTokens(int maxTokens) => _config.MaxCompletionTokens = maxTokens;

        public void SetSystemPrompt(string systemPrompt) => _systemPrompt = systemPrompt;

        public void AddUserMessage(string message)
        {
            lock (_messageLock)
            {
                if (_messagesMemory.Count > 0 && _messagesMemory.Last().Role == "user")
                    _messagesMemory[_messagesMemory.Count - 1] = new GroqCloudMessage("user", _messagesMemory.Last().Content + "\n" + message);
                else
                    _messagesMemory.Add(new GroqCloudMessage("user", message));
            }
        }

        public void AddAssistantMessage(string message)
        {
            lock (_messageLock)
            {
                if (_messagesMemory.Count > 0 && _messagesMemory.Last().Role == "assistant")
                    _messagesMemory[_messagesMemory.Count - 1] = new GroqCloudMessage("assistant", _messagesMemory.Last().Content + "\n" + message);
                else
                    _messagesMemory.Add(new GroqCloudMessage("assistant", message));
            }
        }

        public void EditMessage(int index, string message)
        {
            lock (_messageLock)
            {
                if (index >= 0 && index < _messagesMemory.Count)
                    _messagesMemory[index] = new GroqCloudMessage(_messagesMemory[index].Role, message);
            }
        }

        public void ClearMessages() { lock (_messageLock) _messagesMemory.Clear(); }
        public string GetModel() => _config.Model;
        public string GetProviderFullName() => "GroqCloud";
        public InterfaceLLMProviderEnum GetProviderType() => GetProviderTypeStatic();
        public static InterfaceLLMProviderEnum GetProviderTypeStatic() => InterfaceLLMProviderEnum.GroqCloud;

        public void Dispose()
        {
            ClearMessageStreamed();
            _cts?.Cancel();
            _httpClient.Dispose();
            _cts?.Dispose();
        }
    }
}