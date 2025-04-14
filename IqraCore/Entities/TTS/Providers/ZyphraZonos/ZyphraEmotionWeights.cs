using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.ZyphraZonos
{
    public class ZyphraEmotionWeights
    {
        [JsonPropertyName("happiness")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Happiness { get; set; } // Default 0.6
        [JsonPropertyName("sadness")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Sadness { get; set; } // Default 0.05
        [JsonPropertyName("disgust")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Disgust { get; set; } // Default 0.05
        [JsonPropertyName("fear")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Fear { get; set; } // Default 0.05
        [JsonPropertyName("surprise")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Surprise { get; set; } // Default 0.05
        [JsonPropertyName("anger")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Anger { get; set; } // Default 0.05
        [JsonPropertyName("other")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Other { get; set; } // Default 0.5
        [JsonPropertyName("neutral")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Neutral { get; set; } // Default 0.6
    }
}
