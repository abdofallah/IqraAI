using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Rime
{
    public class RimeTtsRequest
    {
        [JsonPropertyName("speaker")]
        public string Speaker { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("modelId")]
        public string ModelId { get; set; }

        [JsonPropertyName("lang")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Lang { get; set; }

        [JsonPropertyName("samplingRate")]
        public int SamplingRate { get; set; }

        // Arcana Fields
        [JsonPropertyName("repetition_penalty")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? RepetitionPenalty { get; set; }

        [JsonPropertyName("temperature")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? Temperature { get; set; }

        [JsonPropertyName("top_p")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? TopP { get; set; }

        [JsonPropertyName("max_tokens")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxTokens { get; set; }

        // Mist Fields
        [JsonPropertyName("speedAlpha")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? SpeedAlpha { get; set; }

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
    }
}