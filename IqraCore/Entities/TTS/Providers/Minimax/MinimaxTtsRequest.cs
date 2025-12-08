using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Minimax
{
    public class MinimaxTtsRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false; // Use non-streaming for this implementation

        [JsonPropertyName("voice_setting")]
        public MinimaxVoiceSetting VoiceSetting { get; set; } = new();

        [JsonPropertyName("audio_setting")]
        public MinimaxAudioSetting AudioSetting { get; set; } = new();

        [JsonPropertyName("output_format")]
        public string OutputFormat { get; set; } = "hex"; // Required for non-streaming audio data

        [JsonPropertyName("subtitle_enable")]
        public bool SubtitleEnable { get; set; } = false; // Explicitly false unless needed

        [JsonPropertyName("pronunciation_dict")]
        public Dictionary<string, List<string>>? PronunciationDict { get; set; } = null;

        [JsonPropertyName("language_boost")]
        public string LanguageBoost { get; set; } = string.Empty;
    }
}
