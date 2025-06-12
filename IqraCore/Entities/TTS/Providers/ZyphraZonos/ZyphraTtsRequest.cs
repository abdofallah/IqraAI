using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.ZyphraZonos
{
    public class ZyphraTtsRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; set; } = "zonos-v0.1-transformer"; // Default

        [JsonPropertyName("mime_type")]
        public string MimeType { get; set; } = "audio/wav"; // Request WAV

        // Optional parameters
        [JsonPropertyName("speaking_rate")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? SpeakingRate { get; set; } // 5 to 35

        [JsonPropertyName("language_iso_code")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? LanguageIsoCode { get; set; } // e.g., "en-us"

        [JsonPropertyName("default_voice_name")] // Use default voices
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DefaultVoiceName { get; set; }

        [JsonPropertyName("voice_name")] // Use custom trained voices
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? VoiceName { get; set; }

        // Transformer model specific
        [JsonPropertyName("emotion")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, float>? Emotion { get; set; }

        [JsonPropertyName("pitchStd")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? PitchStd { get; set; } // 0 to 100

        // Hybrid model specific
        [JsonPropertyName("vqscore")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Vqscore { get; set; } // e.g., 0.7

        [JsonPropertyName("speaker_noised")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] // Default is false
        public bool SpeakerNoised { get; set; } = false;

        // Note: speaker_audio for voice cloning is not included here for simplicity
    }
}
