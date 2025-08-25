using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.AzureSpeech
{
    public class AzureSpeechConfig : ITTSConfig
    {
        public int ConfigVersion => 1;
        public string Language { get; set; }
        public string VoiceName { get; set; }
        public int TargetSampleRate { get; set; }
        public int TargetBitsPerSample { get; set; }
        public AudioEncodingTypeEnum TargetEncodingType { get; set; }
    }
}
