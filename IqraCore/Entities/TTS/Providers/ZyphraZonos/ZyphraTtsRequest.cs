using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.ZyphraZonos
{
    public class ZyphraTtsRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("mime_type")]
        public string MimeType { get; set; }

        [JsonPropertyName("speaking_rate")]
        public int? SpeakingRate { get; set; }

        [JsonPropertyName("language_iso_code")]
        public string? LanguageIsoCode { get; set; }

        [JsonPropertyName("default_voice_name")]
        public string? DefaultVoiceName { get; set; }

        [JsonPropertyName("fmax")]
        public float? Fmax { get; set; }

        [JsonPropertyName("vqscore")]
        public float? Vqscore { get; set; }

        // Transformer
        [JsonPropertyName("emotion")]
        public Dictionary<string, float>? Emotion { get; set; }

        [JsonPropertyName("pitchStd")]
        public float? PitchStd { get; set; }

        // Hybrid
        [JsonPropertyName("speaker_noised")]
        public bool? SpeakerNoised { get; set; }
    }
}
