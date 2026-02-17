using Azure;
using Azure.AI.Inference;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IqraInfrastructure.Managers.LLM.Providers
{
    public class AzureAIInferenceConfig
    {
        public string ModelDeploymentName { get; set; } = "";
        public float? Temperature { get; set; }
        public float? NucleusSamplingFactor { get; set; } // Top P
        public int? MaxTokens { get; set; }
        public long? Seed { get; set; }
        public float? PresencePenalty { get; set; }
        public float? FrequencyPenalty { get; set; }
        public string? AdditionalPropertiesJson { get; set; }
    }

    public class AzureAIInferenceStreamingLLM : ILLMService
    {
        private readonly ILogger<AzureAIInferenceStreamingLLM> _logger;
        private readonly ChatCompletionsClient _client;
        private readonly CancellationTokenSource _cts;

        private AzureAIInferenceConfig _config;
        private string _systemPrompt;
        private List<ChatRequestMessage> _initialMessages;
        private List<ChatRequestMessage> _messagesMemory;

        public event EventHandler<ConversationAgentEventLLMStreamed>? MessageStreamed;
        public void ClearMessageStreamed() => MessageStreamed = null;

        public event EventHandler<ConversationAgentEventLLMStreamCancelled> MessageStreamedCancelled;

        public AzureAIInferenceStreamingLLM(ILogger<AzureAIInferenceStreamingLLM> logger, string resourceEndpoint, string resourceApiKey, AzureAIInferenceConfig config)
        {
            _logger = logger;
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _cts = new();

            // Enforce minimum tokens
            if (!_config.MaxTokens.HasValue || _config.MaxTokens.Value < 200)
            {
                _config.MaxTokens = 200;
            }

            _client = new ChatCompletionsClient(new Uri(resourceEndpoint), new AzureKeyCredential(resourceApiKey));

            _initialMessages = new List<ChatRequestMessage>();
            _messagesMemory = new List<ChatRequestMessage>();
            _systemPrompt = "You are Iqra. A helpful AI Assistant.";
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


            var options = new ChatCompletionsOptions()
            {
                Messages = finalMessages,
                Model = _config.ModelDeploymentName,
                MaxTokens = _config.MaxTokens,
                Temperature = _config.Temperature,
                NucleusSamplingFactor = _config.NucleusSamplingFactor,
                PresencePenalty = _config.PresencePenalty,
                FrequencyPenalty = _config.FrequencyPenalty,
                Seed = _config.Seed,
                ResponseFormat = ChatCompletionsResponseFormat.CreateTextFormat()
            };

            if (!string.IsNullOrWhiteSpace(_config.AdditionalPropertiesJson))
            {
                try
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(_config.AdditionalPropertiesJson);
                    if (dict != null)
                    {
                        foreach (var kvp in dict)
                        {
                            // Convert object to BinaryData for SDK
                            options.AdditionalProperties[kvp.Key] = BinaryData.FromObjectAsJson(kvp.Value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"Failed to parse Additional Properties JSON: {ex.Message}");
                }
            }

            try
            {
                StreamingResponse<StreamingChatCompletionsUpdate> completionResult = await _client.CompleteStreamingAsync(options, combinedCancellationToken);

                await foreach (var completion in completionResult)
                {
                    MessageStreamed?.Invoke(this, new ConversationAgentEventLLMStreamed(completion));
                }
            }
            catch (Exception ex)
            {
                if (!(ex is TaskCanceledException || ex is OperationCanceledException))
                {
                    _logger?.LogError(ex, "Azure AI Inference Stream Error");
                    MessageStreamedCancelled?.Invoke(this, new ConversationAgentEventLLMStreamCancelled { Type = ConversationAgentEventLLMStreamCancelledTypeEnum.InternalExceptionError, ResponseMessage = ex.Message });
                }
                else
                {
                    MessageStreamedCancelled?.Invoke(this, new ConversationAgentEventLLMStreamCancelled { Type = ConversationAgentEventLLMStreamCancelledTypeEnum.OperationCancelled });
                }
            }
        }

        public void SetModel(string model) => _config.ModelDeploymentName = model;
        public void SetTemperature(decimal temperature) => _config.Temperature = (float)temperature;
        public void SetMaxTokens(int maxTokens) => _config.MaxTokens = maxTokens;
        public void SetSystemPrompt(string systemPrompt) => _systemPrompt = systemPrompt;

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
                var oldMsg = _messagesMemory[index];
                if (oldMsg is ChatRequestUserMessage) _messagesMemory[index] = new ChatRequestUserMessage(message);
                else if (oldMsg is ChatRequestAssistantMessage) _messagesMemory[index] = new ChatRequestAssistantMessage(message);
                else if (oldMsg is ChatRequestSystemMessage) _messagesMemory[index] = new ChatRequestSystemMessage(message);
            }
        }

        public void ClearMessages() => _messagesMemory.Clear();
        public string GetModel() => _config.ModelDeploymentName;
        public string GetProviderFullName() => "Azure AI Inference";
        public InterfaceLLMProviderEnum GetProviderType() => GetProviderTypeStatic();
        public static InterfaceLLMProviderEnum GetProviderTypeStatic() => InterfaceLLMProviderEnum.AzureAIInference;

        public void Dispose()
        {
            ClearMessageStreamed();
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
