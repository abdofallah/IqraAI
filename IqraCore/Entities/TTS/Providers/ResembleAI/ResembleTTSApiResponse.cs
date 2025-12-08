using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.ResembleAI
{
    public class ResembleTTSApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("audio_content")]
        public string? AudioContent { get; set; } // Base64 encoded audio

        [JsonPropertyName("audio_timestamps")]
        public ResembleAudioTimestamps? AudioTimestamps { get; set; }

        [JsonPropertyName("duration")]
        public float? Duration { get; set; } // Duration in seconds

        [JsonPropertyName("issues")]
        public List<string>? Issues { get; set; }

        [JsonPropertyName("output_format")]
        public string? OutputFormat { get; set; } // "wav", "mp3"

        [JsonPropertyName("sample_rate")]
        public float? SampleRate { get; set; } // Sample rate of the audio_content

        [JsonPropertyName("synth_duration")]
        public float? SynthDuration { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("message")] // Error message if success is false
        public string? Message { get; set; }
    }

    public class ResembleAudioTimestamps
    {
        [JsonPropertyName("graph_chars")]
        public List<string>? GraphChars { get; set; }

        [JsonPropertyName("graph_times")]
        public List<List<float>>? GraphTimes { get; set; }

        [JsonPropertyName("phon_chars")]
        public List<string>? PhonChars { get; set; }

        [JsonPropertyName("phon_times")]
        public List<List<float>>? PhonTimes { get; set; }
    }
}
