using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.ResembleAI
{
    public class ResembleAiConfig : ITtsConfig
    {
        public int ConfigVersion => 1;
        public string ProjectUuid { get; set; } // Included for safety, as projects can scope voice/model access
        public string VoiceUuid { get; set; }
        public int TargetSampleRate { get; set; }
    }
}
