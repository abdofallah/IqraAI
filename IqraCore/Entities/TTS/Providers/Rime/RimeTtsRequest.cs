using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Rime
{
    public class RimeTtsRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("speaker")]
        public string Speaker { get; set; }

        [JsonPropertyName("modelId")]
        public string ModelId { get; set; }

        [JsonPropertyName("lang")]
        public string Lang { get; set; }

        [JsonPropertyName("samplingRate")]
        public int SamplingRate { get; set; }

        [JsonPropertyName("speedAlpha")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? SpeedAlpha { get; set; }

        // Arcana Only
        [JsonPropertyName("repetition_penalty")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? RepetitionPenalty { get; set; }

        [JsonPropertyName("temperature")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Temperature { get; set; }

        [JsonPropertyName("top_p")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? TopP { get; set; }

        [JsonPropertyName("max_tokens")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxTokens { get; set; }

        // Mist Only
        [JsonPropertyName("noTextNormalization")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? NoTextNormalization { get; set; }

        [JsonPropertyName("pauseBetweenBrackets")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? PauseBetweenBrackets { get; set; }

        [JsonPropertyName("phonemizeBetweenBrackets")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? PhonemizeBetweenBrackets { get; set; }

        [JsonPropertyName("inlineSpeedAlpha")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? InlineSpeedAlpha { get; set; }

        [JsonPropertyName("saveOovs")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? SaveOovs { get; set; }
    }
}