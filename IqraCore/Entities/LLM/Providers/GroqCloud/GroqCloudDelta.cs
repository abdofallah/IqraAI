using System.Text.Json.Serialization;

namespace IqraCore.Entities.LLM.Providers.GroqCloud
{
    public class GroqCloudDelta
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; } // Usually null after the first chunk

        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
