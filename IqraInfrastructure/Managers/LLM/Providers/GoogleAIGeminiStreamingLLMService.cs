using GenerativeAI;
using GenerativeAI.Types;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.LLM.Providers
{
    public class GoogleAIGeminiConfig
    {
        public string Model { get; set; } = "";
        public double? Temperature { get; set; }
        public double? TopP { get; set; }
        public int? TopK { get; set; }
        public int? MaxOutputTokens { get; set; }
        public int? Seed { get; set; }
        public double? PresencePenalty { get; set; }
        public double? FrequencyPenalty { get; set; }
        public bool? EnableAffectiveDialog { get; set; }

        // Thinking
        public bool? ThinkingIncludeThoughts { get; set; }
        public int? ThinkingBudget { get; set; }

        // Routing
        public string? RoutingPreference { get; set; } // "balanced", "prioritize_quality", "prioritize_cost"
    }

    public class GoogleAIGeminiStreamingLLMService : ILLMService
    {
        private readonly ILogger<GoogleAIGeminiStreamingLLMService> _logger;
        private readonly GoogleAi _client;
        private readonly CancellationTokenSource _cts;

        private GoogleAIGeminiConfig _config;
        private GenerativeModel _googleModel;
        private Content _systemInstruction;
        private List<Content> _initialMessages;
        private List<Content> _messagesMemory;

        public event EventHandler<ConversationAgentEventLLMStreamed>? MessageStreamed;
        public void ClearMessageStreamed() => MessageStreamed = null;

        public event EventHandler<ConversationAgentEventLLMStreamCancelled> MessageStreamedCancelled;

        public GoogleAIGeminiStreamingLLMService(ILogger<GoogleAIGeminiStreamingLLMService> logger, string apiKey, GoogleAIGeminiConfig config)
        {
            _logger = logger;
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _cts = new();

            // Enforce minimum tokens
            if (_config.MaxOutputTokens.HasValue && _config.MaxOutputTokens.Value < 200)
            {
                _config.MaxOutputTokens = 200;
            }

            _client = new GoogleAi(apiKey);
            _googleModel = _client.CreateGenerativeModel(_config.Model);

            _initialMessages = new List<Content>();
            _messagesMemory = new List<Content>();
        }

        public async Task ProcessInputAsync(CancellationToken cancellationToken, string? beforeMessageContext = null, string? afterMessageContext = null)
        {
            var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken).Token;

            var finalMessages = _initialMessages.Concat(_messagesMemory).ToList();

            if (!string.IsNullOrEmpty(beforeMessageContext) || !string.IsNullOrEmpty(afterMessageContext))
            {
                var lastMessage = finalMessages.LastOrDefault();
                if (lastMessage != null && lastMessage.Role == "user")
                {
                    var lastMessageWithText = lastMessage.Parts.Find(x => !string.IsNullOrEmpty(x.Text));
                    if (lastMessageWithText != null)
                    {
                        var newText = lastMessageWithText.Text;
                        if (!string.IsNullOrEmpty(beforeMessageContext)) newText = beforeMessageContext + "\n\n" + newText;
                        if (!string.IsNullOrEmpty(afterMessageContext)) newText = newText + "\n\n" + afterMessageContext;

                        finalMessages.RemoveAt(finalMessages.Count - 1);
                        finalMessages.Add(MakeContent("user", newText));
                    }
                }
            }

            var generationConfig = new GenerationConfig
            {
                Temperature = _config.Temperature,
                TopP = _config.TopP,
                TopK = _config.TopK,
                MaxOutputTokens = _config.MaxOutputTokens,
                Seed = _config.Seed,
                PresencePenalty = _config.PresencePenalty,
                FrequencyPenalty = _config.FrequencyPenalty,
                EnableAffectiveDialog = _config.EnableAffectiveDialog,
            };

            // Map Thinking Config
            if (_config.ThinkingIncludeThoughts == true && _config.ThinkingBudget.HasValue)
            {
                generationConfig.ThinkingConfig = new ThinkingConfig
                {
                    IncludeThoughts = true,
                    ThinkingBudget = _config.ThinkingBudget
                };
            }

            // Map Routing Config
            if (!string.IsNullOrEmpty(_config.RoutingPreference))
            {
                ModelRoutingPreference pref = ModelRoutingPreference.BALANCED;
                if (_config.RoutingPreference == "prioritize_quality") pref = ModelRoutingPreference.PRIORITIZE_QUALITY;
                else if (_config.RoutingPreference == "prioritize_cost") pref = ModelRoutingPreference.PRIORITIZE_COST;

                generationConfig.RoutingConfig = new RoutingConfig
                {
                    AutoMode = new AutoRoutingMode { ModelRoutingPreference = pref }
                };
            }

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
            catch (OperationCanceledException)
            {
                MessageStreamedCancelled?.Invoke(this, new ConversationAgentEventLLMStreamCancelled { Type = ConversationAgentEventLLMStreamCancelledTypeEnum.OperationCancelled });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Google Gemini Stream Error");
                MessageStreamedCancelled?.Invoke(this, new ConversationAgentEventLLMStreamCancelled { Type = ConversationAgentEventLLMStreamCancelledTypeEnum.InternalExceptionError, ResponseMessage = ex.Message });
            }
        }

        public void SetModel(string modelId) { _config.Model = modelId; _googleModel = _client.CreateGenerativeModel(modelId); }
        public void SetTemperature(decimal temperature) => _config.Temperature = (double)temperature;
        public void SetMaxTokens(int maxTokens) => _config.MaxOutputTokens = maxTokens;

        public void SetSystemPrompt(string systemPrompt)
        {
            if (!string.IsNullOrWhiteSpace(systemPrompt)) _systemInstruction = new Content { Parts = { MakeTextPart(systemPrompt) } };
            else _systemInstruction = null;
        }

        public void AddUserMessage(string message) => _messagesMemory.Add(MakeContent("user", message));
        public void AddAssistantMessage(string message) => _messagesMemory.Add(MakeContent("model", message));

        public void EditMessage(int index, string message)
        {
            if (index >= 0 && index < _messagesMemory.Count)
            {
                if (_messagesMemory[index].Role.Equals("model", StringComparison.OrdinalIgnoreCase))
                    _messagesMemory[index] = MakeContent("model", message);
            }
        }

        public void ClearMessages() => _messagesMemory.Clear();
        public string GetModel() => _config.Model;
        public string GetProviderFullName() => "Google Gemini";
        public InterfaceLLMProviderEnum GetProviderType() => GetProviderTypeStatic();
        public static InterfaceLLMProviderEnum GetProviderTypeStatic() => InterfaceLLMProviderEnum.GoogleAIGemini;

        // HELPERS
        private static Part MakeTextPart(string text) => new Part { Text = text };
        private static Content MakeContent(string role, string text) => new Content { Role = role, Parts = { MakeTextPart(text) } };

        public void Dispose()
        {
            ClearMessageStreamed();
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}