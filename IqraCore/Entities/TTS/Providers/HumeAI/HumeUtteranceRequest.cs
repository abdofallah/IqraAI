using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.HumeAI
{
    public class HumeUtteranceRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("voice")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public HumeVoiceSpecifier? Voice { get; set; }

        // Optional acting instructions/voice generation prompt
        [JsonPropertyName("description")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }

        // Optional speed control
        [JsonPropertyName("speed")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Speed { get; set; } // Range 0.25 to 3.0

        // Optional trailing silence
        [JsonPropertyName("trailing_silence")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? TrailingSilence { get; set; } // Seconds
    }
}
