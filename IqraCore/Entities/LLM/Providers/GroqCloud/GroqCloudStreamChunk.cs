using System.Text.Json.Serialization;

namespace IqraCore.Entities.LLM.Providers.GroqCloud
{
    public class GroqCloudStreamChunk
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("object")]
        public string Object { get; set; } // e.g., "chat.completion.chunk"

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("choices")]
        public List<GroqCloudChoice> Choices { get; set; }

        [JsonPropertyName("x_groq")]
        public GroqCloudUsageInfo XGroq { get; set; } // Optional: Capture usage if needed
    }
}
