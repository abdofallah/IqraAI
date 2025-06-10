using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.HumeAI
{
    public class HumeUtteranceRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("voice")]
        public HumeVoiceSpecifier Voice { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("speed")]
        public double Speed { get; set; } 
    }
}
