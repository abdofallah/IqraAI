using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.Sarvam
{
    public class SarvamConfig : ITTSConfig
    {
        public int ConfigVersion => 1;
        public string Model { get; set; }

        public string TargetLanguageCode { get; set; }
        public string Speaker { get; set; }

        public double Pitch { get; set; }
        public double Pace { get; set; }
        public double Loudness { get; set; }
        public bool EnablePreprocessing { get; set; }

        public int TargetSampleRate { get; set; }
        public int TargetBitsPerSample { get; set; }
        public AudioEncodingTypeEnum TargetEncodingType { get; set; }
    }
}