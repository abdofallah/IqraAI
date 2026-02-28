using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.HumeAI
{
    public class HumeAiConfig : ITTSConfig
    {
        public int ConfigVersion => 1;

        public int ModelVersion { get; set; }
        public string? VoiceId { get; set; }
        public string? VoiceProvider { get; set; }
        public string? VoiceDescription { get; set; }
        public float? VoiceSpeed { get; set; }
        public bool InstantMode { get; set; }

        public int TargetSampleRate { get; set; }
        public int TargetBitsPerSample { get; set; }
        public AudioEncodingTypeEnum TargetEncodingType { get; set; }
    }
}
