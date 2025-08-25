using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.Neuphonic
{
    public class NeuphonicConfig : ITTSConfig
    {
        public int ConfigVersion => 1;
        public string LanguageCode { get; set; }
        public string Model { get; set; }
        public string VoiceId { get; set; }
        public float Speed { get; set; }
        public int TargetSampleRate { get; set; }
        public int TargetBitsPerSample { get; set; }
        public AudioEncodingTypeEnum TargetEncodingType { get; set; }
    }
}
