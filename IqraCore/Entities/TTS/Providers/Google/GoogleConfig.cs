using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.Google
{
    public class GoogleConfig : ITTSConfig
    {
        public int ConfigVersion => 1;
        public string LanguageCode { get; set; }
        public string VoiceName { get; set; }
        public float SpeakingRate { get; set; }
        public int TargetSampleRate { get; set; }
        public int TargetBitsPerSample { get; set; }
        public AudioEncodingTypeEnum TargetEncodingType { get; set; }
    }
}
