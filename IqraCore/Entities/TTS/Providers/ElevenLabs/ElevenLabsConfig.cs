using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.ElevenLabs
{
    public class ElevenLabsConfig : ITtsConfig
    {
        public int ConfigVersion => 1;
        public string ModelId { get; set; }
        public string VoiceId { get; set; }
        public float? Stability { get; set; }
        public float? SimilarityBoost { get; set; }
        public float? Style { get; set; }
        public bool? UseSpeakerBoost { get; set; }
        public float? Speed { get; set; }
        public string? PronunciationDictionaryId { get; set; }
        public string? ApplyTextNormalization { get; set; }
        public int TargetSampleRate { get; set; }
        public int TargetBitsPerSample { get; set; }
        public AudioEncodingTypeEnum TargetEncodingType { get; set; }
    }
}
