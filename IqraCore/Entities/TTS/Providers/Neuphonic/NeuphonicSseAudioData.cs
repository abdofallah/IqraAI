using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Neuphonic
{
    public class NeuphonicSseAudioData
    {
        [JsonPropertyName("audio")]
        public string? Audio { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("sampling_rate")]
        public int? SamplingRate { get; set; }
    }
}
