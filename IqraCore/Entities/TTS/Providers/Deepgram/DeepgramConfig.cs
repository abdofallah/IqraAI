using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.Deepgram
{
    public class DeepgramConfig : ITtsConfig
    {
        public int ConfigVersion => 1;
        public string ModelId { get; set; }
        public int SampleRate { get; set; }
    }
}
