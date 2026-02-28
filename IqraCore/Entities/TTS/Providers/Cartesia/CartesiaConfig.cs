using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.Cartesia
{
    public class CartesiaConfig : ITTSConfig
    {
        public int ConfigVersion => 1;
        public string VoiceId { get; set; }
        public string ModelId { get; set; }
        public string LanguageCode { get; set; }

        // Sonic 3 Features
        public double? Volume { get; set; }
        public double? Speed { get; set; }
        public string? Emotion { get; set; }
        public string? PronunciationDictId { get; set; }

        public int TargetSampleRate { get; set; }
        public int TargetBitsPerSample { get; set; }
        public AudioEncodingTypeEnum TargetEncodingType { get; set; }
    }
}
