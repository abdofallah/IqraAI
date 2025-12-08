using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.UpliftAI
{
    public class UpliftTtsRequest
    {
        [JsonPropertyName("voiceId")]
        public string VoiceId { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("outputFormat")]
        public string OutputFormat { get; set; } = "WAV_22050_16";

        [JsonPropertyName("phraseReplacementConfigId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PhraseReplacementConfigId { get; set; }
    }
}