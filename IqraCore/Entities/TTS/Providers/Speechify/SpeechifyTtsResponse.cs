using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Speechify
{
    public class SpeechifyTtsResponse
    {
        [JsonPropertyName("audio_data")]
        public string? AudioData { get; set; } // Base64 encoded audio

        [JsonPropertyName("audio_format")]
        public string? AudioFormat { get; set; } // e.g., "wav", "mp3"

        [JsonPropertyName("billable_characters_count")]
        public long? BillableCharactersCount { get; set; }

        [JsonPropertyName("speech_marks")]
        public SpeechifySpeechMarks? SpeechMarks { get; set; }

        // For handling potential errors in the response body
        [JsonPropertyName("error")]
        public string? ErrorMessage { get; set; }
        [JsonPropertyName("code")] // Assuming potential error code field
        public string? ErrorCode { get; set; }
    }
}