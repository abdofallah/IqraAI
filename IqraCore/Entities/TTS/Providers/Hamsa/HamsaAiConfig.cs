using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.Hamsa
{
    public class HamsaAiConfig : ITtsConfig
    {
        public int ConfigVersion => 1;
        public string Speaker { get; set; }
        public string Dialect { get; set; }
        public int TargetSampleRate { get; set; }
    }
}
