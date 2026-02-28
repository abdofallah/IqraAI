using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.MurfAI
{
    public class MurfStreamTtsRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("voiceId")]
        public string VoiceId { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("multiNativeLocale")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MultiNativeLocale { get; set; }

        [JsonPropertyName("style")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Style { get; set; }

        [JsonPropertyName("rate")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Rate { get; set; }

        [JsonPropertyName("pitch")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Pitch { get; set; }

        [JsonPropertyName("variation")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Variation { get; set; }

        [JsonPropertyName("sampleRate")]
        public int SampleRate { get; set; }

        [JsonPropertyName("format")]
        public string Format { get; set; }

        [JsonPropertyName("channelType")]
        public string ChannelType { get; set; }

        [JsonPropertyName("pronunciationDictionary")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, MurfPronunciationDetail>? PronunciationDictionary { get; set; }
    }
}
