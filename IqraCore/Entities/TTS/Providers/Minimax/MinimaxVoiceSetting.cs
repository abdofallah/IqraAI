using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Minimax
{
    public class MinimaxVoiceSetting
    {
        [JsonPropertyName("voice_id")]
        public string VoiceId { get; set; }

        [JsonPropertyName("speed")]
        public float? Speed { get; set; }

        [JsonPropertyName("vol")]
        public float? Vol { get; set; }

        [JsonPropertyName("pitch")]
        public int? Pitch { get; set; }

        [JsonPropertyName("emotion")]
        public string? Emotion { get; set; }

        [JsonPropertyName("text_normalization")]
        public bool TextNormalization { get; set; }
    }
}
