using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.MurfAI
{
    public class MurfTtsRequest
    {
        [JsonPropertyName("voiceId")]
        public string VoiceId { get; set; }

        [JsonPropertyName("style")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Style { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("rate")]
        public int Rate { get; set; }

        [JsonPropertyName("pitch")]
        public int Pitch { get; set; }

        [JsonPropertyName("sampleRate")]
        public int SampleRate { get; set; }

        [JsonPropertyName("format")]
        public string Format { get; set; } = "PCM";

        [JsonPropertyName("channelType")]
        public string ChannelType { get; set; } = "MONO";

        [JsonPropertyName("encodeAsBase64")]
        public bool EncodeAsBase64 { get; set; } = true;

        [JsonPropertyName("variation")]
        public int Variation { get; set; }

        [JsonPropertyName("modelVersion")]
        public string ModelVersion { get; set; }

        [JsonPropertyName("pronunciationDictionary")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, MurfPronunciationEntry>? PronunciationDictionary { get; set; }
    }

    public class MurfPronunciationEntry
    {
        [JsonPropertyName("pronunciation")]
        public string Pronunciation { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "IPA";
    }

    public class MurfTtsResponse
    {
        [JsonPropertyName("encodedAudio")]
        public string? EncodedAudio { get; set; }

        [JsonPropertyName("audioLengthInSeconds")]
        public double? AudioLengthInSeconds { get; set; }

        [JsonPropertyName("error")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("code")]
        public object? ErrorCode { get; set; } 
    }
}