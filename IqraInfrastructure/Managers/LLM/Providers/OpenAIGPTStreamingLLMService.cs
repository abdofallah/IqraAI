#pragma warning disable OPENAI001

using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;
using System.ClientModel.Primitives;

namespace IqraInfrastructure.Managers.LLM.Providers
{
    public class OpenAIGPTConfig
    {
        public string Model { get; set; } = "";
        public float? Temperature { get; set; }
        public float? TopP { get; set; }
        public int? MaxTokens { get; set; }

        public string? ServiceTier { get; set; } // "default", "flex", "priority"
        public string? ReasoningEffort { get; set; } // "minimal", "low", "medium", "high"
        public string? ReasoningSummary { get; set; } // "auto", "concise", "detailed"
    }

    public class OpenAIGPTStreamingLLMService : ILLMService
    {
        private readonly ILogger<OpenAIGPTStreamingLLMService> _logger;
        private readonly ResponsesClient _client;
        private readonly CancellationTokenSource _cts;

        private OpenAIGPTConfig _config;
        private string _systemPrompt;
        private List<ResponseItem> _messagesMemory;

        public event EventHandler<ConversationAgentEventLLMStreamed>? MessageStreamed;
        public void ClearMessageStreamed() => MessageStreamed = null;

        public event EventHandler<ConversationAgentEventLLMStreamCancelled> MessageStreamedCancelled;

        public OpenAIGPTStreamingLLMService(ILogger<OpenAIGPTStreamingLLMService> logger, string apiKey, string endpoint, OpenAIGPTConfig config)
        {
            _logger = logger;
            _cts = new();
            _config = config ?? throw new ArgumentNullException(nameof(config));

            // Enforce minimum tokens
            if (!_config.MaxTokens.HasValue || _config.MaxTokens.Value < 200)
            {
                _config.MaxTokens = 200;
            }

            OpenAIClient client = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions()
            {
                Endpoint = new Uri(endpoint),
                UserAgentApplicationId = "Iqra.bot",
                NetworkTimeout = TimeSpan.FromSeconds(10),
                RetryPolicy = new ClientRetryPolicy(),
                Transport = new HttpClientPipelineTransport()
            });

            _client = client.GetResponsesClient(_config.Model);
            _messagesMemory = new List<ResponseItem>();
            _systemPrompt = "You are Iqra. A helpful AI Assistant.";
        }

        public async Task ProcessInputAsync(CancellationToken cancellationToken, string? beforeMessageContext = null, string? afterMessageContext = null)
        {
            var combinedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken).Token;

            var request = new CreateResponseOptions()
            {
                MaxOutputTokenCount = _config.MaxTokens,
                Instructions = _systemPrompt,
                Temperature = _config.Temperature,
                TopP = _config.TopP
            };

            // Map Service Tier
            if (!string.IsNullOrEmpty(_config.ServiceTier) && _config.ServiceTier != "default")
            {
                if (_config.ServiceTier == "flex") request.ServiceTier = ResponseServiceTier.Flex;
                else if (_config.ServiceTier == "priority") request.ServiceTier = ResponseServiceTier.Scale;
            }

            // Map Reasoning Options
            if (!string.IsNullOrEmpty(_config.ReasoningEffort) || !string.IsNullOrEmpty(_config.ReasoningSummary))
            {
                request.ReasoningOptions = new ResponseReasoningOptions();

                if (_config.ReasoningEffort == "minimal") request.ReasoningOptions.ReasoningEffortLevel = ResponseReasoningEffortLevel.Minimal;
                else if (_config.ReasoningEffort == "low") request.ReasoningOptions.ReasoningEffortLevel = ResponseReasoningEffortLevel.Low;
                else if (_config.ReasoningEffort == "medium") request.ReasoningOptions.ReasoningEffortLevel = ResponseReasoningEffortLevel.Medium;
                else if (_config.ReasoningEffort == "high") request.ReasoningOptions.ReasoningEffortLevel = ResponseReasoningEffortLevel.High;

                if (_config.ReasoningSummary == "auto") request.ReasoningOptions.ReasoningSummaryVerbosity = ResponseReasoningSummaryVerbosity.Auto;
                else if (_config.ReasoningSummary == "concise") request.ReasoningOptions.ReasoningSummaryVerbosity = ResponseReasoningSummaryVerbosity.Concise;
                else if (_config.ReasoningSummary == "detailed") request.ReasoningOptions.ReasoningSummaryVerbosity = ResponseReasoningSummaryVerbosity.Detailed;
            }

            // Add Memory
            for (int i = 0; i < _messagesMemory.Count; i++)
            {
                request.InputItems.Add(_messagesMemory[i]);
            }

            // Context Injection
            if (!string.IsNullOrEmpty(beforeMessageContext) || !string.IsNullOrEmpty(afterMessageContext))
            {
                var lastMessage = request.InputItems.LastOrDefault();
                if (lastMessage != null && lastMessage is MessageResponseItem userMessageLast && userMessageLast.Role == MessageRole.User)
                {
                    var newText = userMessageLast.Content.FirstOrDefault()?.Text;
                    if (!string.IsNullOrEmpty(beforeMessageContext)) newText = beforeMessageContext + "\n\n" + newText;
                    if (!string.IsNullOrEmpty(afterMessageContext)) newText = newText + "\n\n" + afterMessageContext;

                    var newUserMessage = ResponseItem.CreateUserMessageItem(newText);
                    request.InputItems.RemoveAt(request.InputItems.Count - 1);
                    request.InputItems.Add(newUserMessage);
                }
            }

            try
            {
                var completionResult = _client.CreateResponseStreamingAsync(request, combinedCancellationToken);

                await foreach (var StreamingResponseUpdate in completionResult)
                {
                    MessageStreamed?.Invoke(this, new ConversationAgentEventLLMStreamed(StreamingResponseUpdate));
                }
            }
            catch (Exception ex)
            {
                if (!(ex is TaskCanceledException || ex is OperationCanceledException))
                {
                    MessageStreamedCancelled?.Invoke(this, new ConversationAgentEventLLMStreamCancelled { Type = ConversationAgentEventLLMStreamCancelledTypeEnum.InternalExceptionError, ResponseMessage = ex.Message });
                    _logger?.LogError(ex, "OpenAI Stream Error");
                }
                else
                {
                    MessageStreamedCancelled?.Invoke(this, new ConversationAgentEventLLMStreamCancelled { Type = ConversationAgentEventLLMStreamCancelledTypeEnum.OperationCancelled });
                }
            }
        }

        public void SetModel(string model) => _config.Model = model;
        public void SetTemperature(decimal temperature) => _config.Temperature = (float)temperature;
        public void SetMaxTokens(int maxTokens) => _config.MaxTokens = maxTokens;
        public void SetSystemPrompt(string systemPrompt) => _systemPrompt = systemPrompt;

        public void AddUserMessage(string message) => _messagesMemory.Add(ResponseItem.CreateUserMessageItem(message));
        public void AddAssistantMessage(string message) => _messagesMemory.Add(ResponseItem.CreateAssistantMessageItem(message));

        public void EditMessage(int index, string message)
        {
            if (index >= 0 && index < _messagesMemory.Count)
            {
                if (_messagesMemory[index] is MessageResponseItem messageItem)
                {
                    if (messageItem.Role == MessageRole.User) _messagesMemory[index] = ResponseItem.CreateUserMessageItem(message);
                    else if (messageItem.Role == MessageRole.Assistant) _messagesMemory[index] = ResponseItem.CreateAssistantMessageItem(message);
                }
            }
        }

        public void ClearMessages() => _messagesMemory.Clear();
        public string GetModel() => _config.Model;
        public string GetProviderFullName() => "OpenAI GPT";
        public InterfaceLLMProviderEnum GetProviderType() => GetProviderTypeStatic();
        public static InterfaceLLMProviderEnum GetProviderTypeStatic() => InterfaceLLMProviderEnum.OpenAIGPT;

        public void Dispose()
        {
            ClearMessageStreamed();
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}