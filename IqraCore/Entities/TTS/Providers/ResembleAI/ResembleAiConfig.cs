using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.ResembleAI
{
    public class ResembleAiConfig : ITTSConfig
    {
        public int ConfigVersion => 1;

        public string Model { get; set; }
        public string VoiceUuid { get; set; }
        public bool UseHd { get; set; }

        public int TargetSampleRate { get; set; }
        public int TargetBitsPerSample { get; set; }
        public AudioEncodingTypeEnum TargetEncodingType { get; set; }
    }
}
