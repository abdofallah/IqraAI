using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Minimax
{
    public class MinimaxAudioSetting
    {
        [JsonPropertyName("sample_rate")]
        public int SampleRate { get; set; } = 16000; // Defaulting to 16000 to match others

        [JsonPropertyName("bitrate")]
        public int Bitrate { get; set; } = 128000; // Default based on docs

        [JsonPropertyName("format")]
        public string Format { get; set; } = "pcm"; // Request raw PCM

        [JsonPropertyName("channel")]
        public int Channel { get; set; } = 1; // Default to mono
    }
}
