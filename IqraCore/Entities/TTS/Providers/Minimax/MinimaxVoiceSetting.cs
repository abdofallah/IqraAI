using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Minimax
{
    public class MinimaxVoiceSetting
    {
        [JsonPropertyName("voice_id")]
        public string VoiceId { get; set; } = string.Empty;

        [JsonPropertyName("speed")]
        public double Speed { get; set; } = 1.0;

        [JsonPropertyName("vol")]
        public double Vol { get; set; } = 1.0;

        [JsonPropertyName("pitch")]
        public int Pitch { get; set; } = 0;

        // Optional fields like emotion, english_normalization can be added here if needed
        [JsonPropertyName("emotion")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Emotion { get; set; }

        [JsonPropertyName("english_normalization")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] // default is false
        public bool EnglishNormalization { get; set; } = false;
    }
}
