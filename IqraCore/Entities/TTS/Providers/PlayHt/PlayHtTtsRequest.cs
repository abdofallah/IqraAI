using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.PlayHt
{
    public class PlayHtTtsRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("voice")]
        public string Voice { get; set; } = string.Empty; // Voice ID/URL/Name

        [JsonPropertyName("output_format")]
        public string OutputFormat { get; set; } = "wav"; // Request WAV

        [JsonPropertyName("sample_rate")]
        public int SampleRate { get; set; } = 16000; // Request target sample rate

        [JsonPropertyName("voice_engine")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? VoiceEngine { get; set; } // e.g., "PlayDialog", "Play3.0-mini"

        [JsonPropertyName("speed")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Speed { get; set; } // 0.1 to 5.0

        // Add other optional parameters like quality, seed, temperature, emotion, etc. if needed
        [JsonPropertyName("quality")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Quality { get; set; } // e.g., "draft", "standard", "premium"

        [JsonPropertyName("temperature")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Temperature { get; set; } // 0 to 2.0

        [JsonPropertyName("emotion")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Emotion { get; set; }
    }
}