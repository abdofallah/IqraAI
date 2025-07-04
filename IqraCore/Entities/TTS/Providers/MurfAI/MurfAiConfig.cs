using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.MurfAI
{
    public class MurfAiConfig : ITtsConfig
    {
        public int ConfigVersion => 1;
        public string Model { get; set; }
        public string VoiceId { get; set; }
        public string MultiNativeLocale { get; set; }
        public string PronunciationDictionaryString { get; set; }
        public int Rate { get; set; }
        public string Style { get; set; }
        public int Variation { get; set; }
        public int SampleRate { get; set; }
    }
}
