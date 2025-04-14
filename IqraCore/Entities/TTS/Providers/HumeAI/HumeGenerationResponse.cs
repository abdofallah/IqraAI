using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.HumeAI
{
    public class HumeGenerationResponse
    {
        [JsonPropertyName("audio")]
        public string? Audio { get; set; } // Base64 encoded audio

        [JsonPropertyName("duration")]
        public double? Duration { get; set; } // Seconds

        [JsonPropertyName("encoding")]
        public HumeEncodingResponse? Encoding { get; set; }

        [JsonPropertyName("file_size")]
        public long? FileSize { get; set; } // Bytes

        [JsonPropertyName("generation_id")]
        public string? GenerationId { get; set; }

        // Snippets are also included, but not strictly needed for this implementation
        // [JsonPropertyName("snippets")]
        // public List<object>? Snippets { get; set; }
    }
}
