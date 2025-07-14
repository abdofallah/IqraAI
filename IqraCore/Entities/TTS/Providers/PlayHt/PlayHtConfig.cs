using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.PlayHt
{
    public class PlayHtConfig : ITtsConfig
    {
        public int ConfigVersion => 1;
        public string VoiceId { get; set; }
        public string VoiceEngine { get; set; }
        public string VoiceQuality { get; set; }
        public float VoiceSpeed { get; set; }
        public float Temperature { get; set; }
        public string Emotion { get; set; }
        public float VoiceGuidance { get; set; }
        public float StyleGuidance { get; set; }
        public float TextGuidance { get; set; }
        public string Language { get; set; }
        public int TargetSampleRate { get; set; }
        public int TargetBitsPerSample { get; set; }
        public AudioEncodingTypeEnum TargetEncodingType { get; set; }
    }
}
