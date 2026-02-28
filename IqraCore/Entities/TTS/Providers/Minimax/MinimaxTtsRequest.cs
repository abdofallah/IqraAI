using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Minimax
{
    public class MinimaxTtsRequest
    {
        [JsonPropertyName("model")] public string Model { get; set; }
        [JsonPropertyName("text")] public string Text { get; set; }
        [JsonPropertyName("stream")] public bool Stream { get; set; }
        [JsonPropertyName("language_boost")] public string LanguageBoost { get; set; }
        [JsonPropertyName("output_format")] public string OutputFormat { get; set; }
        [JsonPropertyName("voice_setting")] public MinimaxVoiceSetting VoiceSetting { get; set; }
        [JsonPropertyName("audio_setting")] public MinimaxAudioSetting AudioSetting { get; set; }
        [JsonPropertyName("voice_modify")] public MinimaxVoiceModify? VoiceModify { get; set; }
        [JsonPropertyName("pronunciation_dict")] public MinimaxPronunciationDict? PronunciationDict { get; set; }
    }
}
