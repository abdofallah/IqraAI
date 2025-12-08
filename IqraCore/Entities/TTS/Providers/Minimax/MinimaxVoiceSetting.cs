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

        [JsonPropertyName("emotion")]
        public List<string> Emotion { get; set; } = new List<string>();

        [JsonPropertyName("text_normalization")]
        public bool TextNormalization { get; set; } = false;
        
        [JsonPropertyName("latex_read")]
        public bool LatexRead { get; set; } = false;
    }
}
