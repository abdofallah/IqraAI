using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.ZyphraZonos
{
    public class ZyphraZonosConfig : ITtsConfig
    {
        public int ConfigVersion => 1;
        public string Model { get; set; }
        public string DefaultVoiceName { get; set; }
        public int SpeakingRate { get; set; }
        public string LanguageIsoCode { get; set; }
        public Dictionary<string, float> Emotion { get; set; } = new Dictionary<string, float>();
        public float Vqscore { get; set; }
        public int TargetSampleRate { get; set; }
        public int TargetBitsPerSample { get; set; }
        public AudioEncodingTypeEnum TargetEncodingType { get; set; }
    }
}
