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

        // V2 Fields
        public float? Pitch { get; set; }
        public float? Loudness { get; set; }
        public bool? EnablePreprocessing { get; set; }
        public float? PaceV2 { get; set; }

        // V3 Fields
        public float? Temperature { get; set; }
        public float? PaceV3 { get; set; }

        public int TargetSampleRate { get; set; }
        public int TargetBitsPerSample { get; set; }
        public AudioEncodingTypeEnum TargetEncodingType { get; set; }
    }
}