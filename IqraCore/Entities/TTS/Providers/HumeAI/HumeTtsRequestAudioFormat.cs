using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.HumeAI
{
    public class HumeTtsRequestAudioFormat
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "pcm";
    }
}
