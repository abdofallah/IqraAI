using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Cartesia
{
    public class CartesiaTtsBytesRequest
    {
        [JsonPropertyName("model_id")]
        public string ModelId { get; set; } = string.Empty;

        [JsonPropertyName("transcript")]
        public string Transcript { get; set; } = string.Empty;

        [JsonPropertyName("voice")]
        public CartesiaVoiceRequest Voice { get; set; } = new();

        [JsonPropertyName("output_format")]
        public CartesiaOutputFormatRequest OutputFormat { get; set; } = new();

        [JsonPropertyName("language")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Language { get; set; }
    }
}
