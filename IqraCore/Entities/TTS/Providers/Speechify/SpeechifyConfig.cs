using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.Speechify
{
    public class SpeechifyConfig : ITTSConfig
    {
        public int ConfigVersion => 1;
        public string VoiceId { get; set; }
        public string Model { get; set; }
        public string Language { get; set; }
        public bool LoudnessNormalization { get; set; }
        public bool TextNormalization { get; set; }
        public int TargetSampleRate { get; set; }
        public int TargetBitsPerSample { get; set; }
        public AudioEncodingTypeEnum TargetEncodingType { get; set; }
    }
}
