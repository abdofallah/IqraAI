using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.AzureSpeech
{
    public class AzureSpeechConfig : ITtsConfig
    {
        public int ConfigVersion => 1;
        public string Language { get; set; }
        public string VoiceName { get; set; }
        public int SampleRate { get; set; }
    }
}
