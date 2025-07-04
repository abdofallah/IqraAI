using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.HumeAI
{
    public class HumeAiConfig : ITtsConfig
    {
        public int ConfigVersion => 1;
        public string VoiceId { get; set; }
        public string VoiceProvider { get; set; }
        public string VoiceDescription { get; set; }
        public float VoiceSpeed { get; set; }
        public int SampleRate { get; set; }
    }
}
