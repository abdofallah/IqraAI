using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.ZyphraZonos
{
    public class ZyphraZonosConfig : ITTSConfig
    {
        public int ConfigVersion => 1;

        public string Model { get; set; }
        public string DefaultVoiceName { get; set; }
        public string? LanguageIsoCode { get; set; }
        public int? SpeakingRate { get; set; }
        public float? Vqscore { get; set; }
        public float? Fmax { get; set; }

        // Transformer only
        public string? EmotionJson { get; set; }
        public float? PitchStd { get; set; }

        // Hybrid only
        public bool? SpeakerNoised { get; set; }

        public int TargetSampleRate { get; set; }
        public int TargetBitsPerSample { get; set; }
        public AudioEncodingTypeEnum TargetEncodingType { get; set; }
    }
}
