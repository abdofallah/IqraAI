using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.Minimax
{
    public class MinimaxConfig : ITtsConfig
    {
        public int ConfigVersion => 1;
        public string ModelId { get; set; }
        public string VoiceId { get; set; }
        public float VoiceSpeed { get; set; }
        public string? LanguageBoostId { get; set; }
        public string? PronunciationDict { get; set; }
        public int SampleRate { get; set; }
    }
}
