using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Speechify
{
    // Simplified representation, add more fields if needed
    public class SpeechifySpeechMarkChunk
    {
        [JsonPropertyName("end")]
        public long? End { get; set; }
        [JsonPropertyName("end_time")]
        public double? EndTime { get; set; }
        [JsonPropertyName("start")]
        public long? Start { get; set; }
        [JsonPropertyName("start_time")]
        public double? StartTime { get; set; }
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }
}