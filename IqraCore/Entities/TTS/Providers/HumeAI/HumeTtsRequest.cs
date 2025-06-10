using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.HumeAI
{
    public class HumeTtsRequest
    {
        [JsonPropertyName("utterances")]
        public List<HumeUtteranceRequest> Utterances { get; set; } = new();

        [JsonPropertyName("context")]
        public HumeTtsRequestContext? Context { get; set; }

        [JsonPropertyName("format")]
        public HumeTtsRequestAudioFormat AudioFormat { get; set; } = new();
    }
}
