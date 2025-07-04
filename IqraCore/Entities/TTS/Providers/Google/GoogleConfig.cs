using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.Google
{
    public class GoogleConfig : ITtsConfig
    {
        public int ConfigVersion => 1;
        public string LanguageCode { get; set; }
        public string VoiceName { get; set; }
        public float SpeakingRate { get; set; }
        public int SampleRate { get; set; }
    }
}
