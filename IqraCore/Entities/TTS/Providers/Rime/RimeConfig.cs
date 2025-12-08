using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.Rime
{
    public class RimeConfig : ITTSConfig
    {
        public int ConfigVersion => 1;

        public string ModelId { get; set; }
        public string Speaker { get; set; }
        public string Lang { get; set; }

        // Arcana Specific
        public float? RepetitionPenalty { get; set; }
        public float? Temperature { get; set; }
        public float? TopP { get; set; }
        public int? MaxTokens { get; set; }

        // Mist Specific
        public float? SpeedAlpha { get; set; } // 1.0 default
        public string? InlineSpeedAlpha { get; set; }
        public bool? NoTextNormalization { get; set; }
        public bool? PauseBetweenBrackets { get; set; }
        public bool? PhonemizeBetweenBrackets { get; set; }

        // Audio Target
        public int TargetSampleRate { get; set; }
        public int TargetBitsPerSample { get; set; }
        public AudioEncodingTypeEnum TargetEncodingType { get; set; }
    }
}