using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.Cartesia
{
    public class CartesiaConfig : ITtsConfig
    {
        public int ConfigVersion => 1;
        public string VoiceId { get; set; }
        public string ModelId { get; set; }
        public string LanguageCode { get; set; }
        public List<string> PronunciationDictIds { get; set; } = new List<string>();
        public int SampleRate { get; set; }
    }
}
