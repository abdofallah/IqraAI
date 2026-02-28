using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Sarvam
{
    // WebSocket Messages
    public class SarvamWsMessage<T>
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("data")]
        public T Data { get; set; }
    }

    // Config Data
    public class SarvamConfigData
    {
        [JsonPropertyName("model")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Model { get; set; }

        [JsonPropertyName("target_language_code")]
        public string TargetLanguageCode { get; set; }

        [JsonPropertyName("speaker")]
        public string Speaker { get; set; }

        [JsonPropertyName("speech_sample_rate")]
        public string SpeechSampleRate { get; set; }

        [JsonPropertyName("output_audio_codec")]
        public string OutputAudioCodec { get; set; }

        // Optional parameters (some model specific)
        [JsonPropertyName("pitch")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? Pitch { get; set; }

        [JsonPropertyName("pace")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? Pace { get; set; }

        [JsonPropertyName("loudness")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? Loudness { get; set; }

        [JsonPropertyName("temperature")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? Temperature { get; set; }

        [JsonPropertyName("enable_preprocessing")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? EnablePreprocessing { get; set; }
    }

    // Text Data
    public class SarvamTextData
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    // Response Data (Audio)
    public class SarvamAudioData
    {
        [JsonPropertyName("audio")]
        public string? Audio { get; set; } // Base64

        [JsonPropertyName("content_type")]
        public string? ContentType { get; set; }
    }

    // Response Data (Event)
    public class SarvamEventData
    {
        [JsonPropertyName("event_type")]
        public string? EventType { get; set; } // "final"
    }

    // Response Data (Error)
    public class SarvamErrorData
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("code")]
        public int? Code { get; set; }
    }
}