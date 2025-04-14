using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Minimax
{
    public class MinimaxResponseExtraInfo
    {
        [JsonPropertyName("audio_length")]
        public long AudioLength { get; set; } // Milliseconds

        [JsonPropertyName("audio_sample_rate")]
        public long AudioSampleRate { get; set; }

        [JsonPropertyName("audio_size")]
        public long AudioSize { get; set; } // Bytes

        [JsonPropertyName("audio_bitrate")]
        public long AudioBitrate { get; set; }

        [JsonPropertyName("audio_format")]
        public string? AudioFormat { get; set; }

        [JsonPropertyName("audio_channel")]
        public int AudioChannel { get; set; }

        [JsonPropertyName("invisible_character_ratio")]
        public double InvisibleCharacterRatio { get; set; }

        [JsonPropertyName("usage_characters")]
        public long UsageCharacters { get; set; }
    }
}
