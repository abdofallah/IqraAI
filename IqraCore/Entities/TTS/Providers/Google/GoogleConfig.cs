using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.Google
{
    public class GoogleConfig : ITTSConfig
    {
        public int ConfigVersion => 1;
        public string LanguageCode { get; set; }
        public string VoiceName { get; set; }

        public double SpeakingRate { get; set; } = 1.0;
        public double Pitch { get; set; } = 0.0;

        public int TargetSampleRate { get; set; }
        public int TargetBitsPerSample { get; set; }
        public AudioEncodingTypeEnum TargetEncodingType { get; set; }
    }
}
