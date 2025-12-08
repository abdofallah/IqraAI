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
        public int VoiceVolume { get; set; }
        public int VoicePitch { get; set; }
        public List<string> VoiceEmotions { get; set; }
        public bool VoiceTextNormalization { get; set; }
        public bool VoiceLatexRead { get; set; }
        public string LanguageBoost { get; set; }
        public Dictionary<string, List<string>>? PronunciationDict { get; set; }
        public int TargetSampleRate { get; set; }
        public int TargetBitsPerSample { get; set; }
        public AudioEncodingTypeEnum TargetEncodingType { get; set; }
    }
}
