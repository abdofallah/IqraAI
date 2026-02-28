using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.MurfAI
{
    public class MurfAiConfig : ITTSConfig
    {
        public int ConfigVersion => 1;

        public string Model { get; set; }
        public string Region { get; set; }
        public string VoiceId { get; set; }
        public string? MultiNativeLocale { get; set; }
        public string? PronunciationDictionaryJson { get; set; }
        public int? Pitch { get; set; }
        public int? Rate { get; set; }
        public string? Style { get; set; }
        public int? Variation { get; set; }

        public int TargetSampleRate { get; set; }
        public int TargetBitsPerSample { get; set; }
        public AudioEncodingTypeEnum TargetEncodingType { get; set; }
    }
}
