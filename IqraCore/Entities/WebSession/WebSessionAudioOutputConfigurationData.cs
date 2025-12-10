using IqraCore.Entities.Helper.Audio;

namespace IqraCore.Entities.WebSession
{
    public class WebSessionAudioOutputConfigurationData
    {
        public int SampleRate { get; set; } = 0;
        public int BitsPerSample { get; set; } = 0;
        public AudioEncodingTypeEnum AudioEncodingType { get; set; } = AudioEncodingTypeEnum.PCM;
        public int FrameDurationMs { get; set; } = 0;
    }
}
