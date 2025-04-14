using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.MurfAI
{
    public class MurfTtsGenerateResponse
    {
        [JsonPropertyName("encodedAudio")]
        public string? EncodedAudio { get; set; } // Base64 audio

        [JsonPropertyName("audioFile")]
        public string? AudioFile { get; set; } // URL (we ignore this if encodedAudio is present)

        [JsonPropertyName("audioLengthInSeconds")]
        public double? AudioLengthInSeconds { get; set; }

        [JsonPropertyName("consumedCharacterCount")]
        public long? ConsumedCharacterCount { get; set; }

        [JsonPropertyName("remainingCharacterCount")]
        public long? RemainingCharacterCount { get; set; }

        [JsonPropertyName("warning")]
        public string? Warning { get; set; }

        [JsonPropertyName("wordDurations")]
        public List<MurfWordDuration>? WordDurations { get; set; }

        // For potential errors within a successful HTTP response
        [JsonPropertyName("error")]
        public string? ErrorMessage { get; set; }
        [JsonPropertyName("code")]
        public int? ErrorCode { get; set; } // Assuming potential error code field
    }
}