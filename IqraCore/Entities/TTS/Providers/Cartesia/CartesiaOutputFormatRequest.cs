using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Cartesia
{
    public class CartesiaOutputFormatRequest
    {
        [JsonPropertyName("container")]
        public string Container { get; set; } = "raw";

        [JsonPropertyName("encoding")]
        public string Encoding { get; set; } = "pcm_s16le";

        [JsonPropertyName("sample_rate")]
        public int SampleRate { get; set; }
    }
}
