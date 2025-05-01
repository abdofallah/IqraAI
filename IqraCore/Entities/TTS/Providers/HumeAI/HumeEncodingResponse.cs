using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.HumeAI
{
    public class HumeEncodingResponse
    {
        [JsonPropertyName("format")]
        public string? Format { get; set; }

        [JsonPropertyName("sample_rate")]
        public int? SampleRate { get; set; }
    }
}
