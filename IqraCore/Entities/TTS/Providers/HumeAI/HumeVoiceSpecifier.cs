using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.HumeAI
{
    public class HumeVoiceSpecifier
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("provider")]
        public string Provider { get; set; }
    }
}
