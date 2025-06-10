using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.PlayHt
{
    public class PlayHtTtsRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("voice")]
        public string Voice { get; set; } = string.Empty; // Voice ID/URL/Name

        [JsonPropertyName("quality")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Quality { get; set; }

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

        [JsonPropertyName("temperature")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Temperature { get; set; } // 0 to 2.0

        [JsonPropertyName("emotion")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Emotion { get; set; }

        //voice_guidance
        [JsonPropertyName("voice_guidance")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? VoiceGuidance { get; set; }

        //style_guidance
        [JsonPropertyName("style_guidance")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? StyleGuidance { get; set; }

        //text_guidance
        [JsonPropertyName("text_guidance")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? TextGuidance { get; set; }

        //language
        [JsonPropertyName("language")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Language { get; set; }
    }
}