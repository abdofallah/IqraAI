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
        [JsonPropertyName("target_language_code")]
        public string TargetLanguageCode { get; set; }

        [JsonPropertyName("speaker")]
        public string Speaker { get; set; }

        [JsonPropertyName("speech_sample_rate")]
        public int SpeechSampleRate { get; set; }

        [JsonPropertyName("output_audio_codec")]
        public string OutputAudioCodec { get; set; } = "linear16"; // We force linear16

        [JsonPropertyName("enable_preprocessing")]
        public bool EnablePreprocessing { get; set; }

        [JsonPropertyName("pitch")]
        public double Pitch { get; set; }

        [JsonPropertyName("pace")]
        public double Pace { get; set; }

        [JsonPropertyName("loudness")]
        public double Loudness { get; set; }
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