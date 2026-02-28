using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Neuphonic
{
    public class NeuphonicSseEventPayload
    {
        [JsonPropertyName("status_code")]
        public int StatusCode { get; set; }

        [JsonPropertyName("errors")]
        public List<string>? Errors { get; set; }

        [JsonPropertyName("data")]
        public NeuphonicSseAudioData? Data { get; set; }
    }
}
