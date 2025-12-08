using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.UpliftAI
{
    public class UpliftAiConfig : ITTSConfig
    {
        public int ConfigVersion => 1;

        public string VoiceId { get; set; }

        // Audio Target
        public int TargetSampleRate { get; set; }
        public int TargetBitsPerSample { get; set; }
        public AudioEncodingTypeEnum TargetEncodingType { get; set; }
    }
}