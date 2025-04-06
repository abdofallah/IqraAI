using System.Text.Json.Serialization;

namespace IqraCore.Entities.LLM.Providers.GroqCloud
{
    public class GroqCloudChoice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("delta")]
        public GroqCloudDelta Delta { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; } // e.g., "stop", "length"
    }
}
