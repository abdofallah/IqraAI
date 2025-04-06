using System.Text.Json.Serialization;

namespace IqraCore.Entities.LLM.Providers.GroqCloud
{
    public class GroqCloudStreamUsage
    {
        [JsonPropertyName("queue_time")]
        public double QueueTime { get; set; }
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }
        [JsonPropertyName("prompt_time")]
        public double PromptTime { get; set; }
        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }
        [JsonPropertyName("completion_time")]
        public double CompletionTime { get; set; }
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
        [JsonPropertyName("total_time")]
        public double TotalTime { get; set; }
    }
}
