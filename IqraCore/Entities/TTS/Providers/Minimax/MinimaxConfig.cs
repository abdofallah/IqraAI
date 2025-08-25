using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.Minimax
{
    public class MinimaxConfig : ITTSConfig
    {
        public int ConfigVersion => 1;
        public string ModelId { get; set; }
        public string VoiceId { get; set; }
        public float VoiceSpeed { get; set; }
        public string? LanguageBoostId { get; set; }
        public string? PronunciationDict { get; set; }
        public int TargetSampleRate { get; set; }
        public int TargetBitsPerSample { get; set; }
        public AudioEncodingTypeEnum TargetEncodingType { get; set; }
    }
}
