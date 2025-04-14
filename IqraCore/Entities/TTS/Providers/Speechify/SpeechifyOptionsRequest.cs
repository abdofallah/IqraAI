using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Speechify
{
    public class SpeechifyOptionsRequest
    {
        [JsonPropertyName("loudness_normalization")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool LoudnessNormalization { get; set; } = false;

        [JsonPropertyName("text_normalization")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool TextNormalization { get; set; } = true; // Default is true per docs
    }
}