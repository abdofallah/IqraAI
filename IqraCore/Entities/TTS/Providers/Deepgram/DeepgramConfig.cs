using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.Deepgram
{
    public class DeepgramConfig : ITTSConfig
    {
        public int ConfigVersion => 1;

        public string ModelFamily { get; set; }
        public string VoiceName { get; set; }
        public string LanguageCode { get; set; }
        
        public int TargetSampleRate { get; set; }
        public int TargetBitsPerSample { get; set; }
        public AudioEncodingTypeEnum TargetEncodingType { get; set; }
    }
}
