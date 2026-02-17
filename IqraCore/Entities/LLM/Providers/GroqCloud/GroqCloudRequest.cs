using System.Text.Json.Serialization;

namespace IqraCore.Entities.LLM.Providers.GroqCloud
{
    public class GroqCloudRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("messages")]
        public List<GroqCloudMessage> Messages { get; set; }

        [JsonPropertyName("temperature")]
        public float? Temperature { get; set; }

        [JsonPropertyName("top_p")]
        public float? TopP { get; set; }

        [JsonPropertyName("max_completion_tokens")]
        public int? MaxCompletionTokens { get; set; }

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }

        [JsonPropertyName("seed")]
        public int? Seed { get; set; }

        [JsonPropertyName("service_tier")]
        public string? ServiceTier { get; set; }

        // Reasoning fields
        [JsonPropertyName("include_reasoning")]
        public bool? IncludeReasoning { get; set; }

        [JsonPropertyName("reasoning_format")]
        public string? ReasoningFormat { get; set; }

        [JsonPropertyName("reasoning_effort")]
        public string? ReasoningEffort { get; set; }

        [JsonPropertyName("stop")]
        public string? Stop { get; set; }
    }
}