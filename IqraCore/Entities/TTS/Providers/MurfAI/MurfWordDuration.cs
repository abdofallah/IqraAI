using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.MurfAI
{
    public class MurfWordDuration
    {
        [JsonPropertyName("word")]
        public string? Word { get; set; }
        [JsonPropertyName("startMs")]
        public int? StartMs { get; set; }
        [JsonPropertyName("endMs")]
        public int? EndMs { get; set; }
        [JsonPropertyName("sourceWordIndex")]
        public int? SourceWordIndex { get; set; }
        [JsonPropertyName("pitchScaleMinimum")]
        public double? PitchScaleMinimum { get; set; }
        [JsonPropertyName("pitchScaleMaximum")]
        public double? PitchScaleMaximum { get; set; }
    }
}