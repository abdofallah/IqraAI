using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.MurfAI
{
    public class MurfTtsGenerateRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("voiceId")]
        public string VoiceId { get; set; } = string.Empty;

        [JsonPropertyName("format")]
        public string Format { get; set; } = "ULAW";

        [JsonPropertyName("sampleRate")]
        public int SampleRate { get; set; } = 8000;

        [JsonPropertyName("channelType")]
        public string ChannelType { get; set; } = "MONO";

        [JsonPropertyName("encodeAsBase64")]
        public bool EncodeAsBase64 { get; set; } = true;

        [JsonPropertyName("modelVersion")]
        public string ModelVersion { get; set; } = "GEN2";

        // Optional parameters can be added here
        [JsonPropertyName("pitch")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Pitch { get; set; } // -50 to 50

        [JsonPropertyName("rate")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Rate { get; set; } // -50 to 50

        [JsonPropertyName("style")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Style { get; set; }

        [JsonPropertyName("variation")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Variation { get; set; } // 0 to 5

        [JsonPropertyName("pronunciationDictionary")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, MurfPronunciationEntry>? PronunciationDictionary { get; set; }
    }

    public class MurfPronunciationEntry
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("pronunciation")]
        public string Pronunciation { get; set; } = string.Empty;
    }
}