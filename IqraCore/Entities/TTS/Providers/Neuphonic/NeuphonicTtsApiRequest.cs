using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Neuphonic
{
    public class NeuphonicTtsApiRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("voice_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? VoiceId { get; set; }

        [JsonPropertyName("sampling_rate")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? SamplingRate { get; set; }

        [JsonPropertyName("speed")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? Speed { get; set; }
    }
}
