using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Neuphonic
{
    public class NeuphonicTtsApiRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("voice_id")]
        public string VoiceId { get; set; }

        [JsonPropertyName("sampling_rate")]
        public int SamplingRate { get; set; }

        [JsonPropertyName("speed")]
        public float? Speed { get; set; }

        [JsonPropertyName("temperature")]
        public float? Temperature { get; set; }

        [JsonPropertyName("encoding")]
        public string Encoding { get; set; }
    }
}
