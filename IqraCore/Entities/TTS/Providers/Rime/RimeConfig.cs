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
        public double? SpeedAlpha { get; set; }

        // Arcana Only
        public int? MaxTokens { get; set; }
        public double? RepetitionPenalty { get; set; }
        public double? Temperature { get; set; }
        public double? TopP { get; set; }

        // Mist Only
        public bool? PauseBetweenBrackets { get; set; }
        public bool? PhonemizeBetweenBrackets { get; set; }
        public string? InlineSpeedAlpha { get; set; }
        public bool? NoTextNormalization { get; set; }
        public bool? SaveOovs { get; set; }

        // Audio Target
        public int TargetSampleRate { get; set; }
        public int TargetBitsPerSample { get; set; }
        public AudioEncodingTypeEnum TargetEncodingType { get; set; }
    }
}