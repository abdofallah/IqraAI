using IqraCore.Entities.Helper.Audio;

namespace IqraCore.Entities.WebSession
{
    public class WebSessionAudioInputConfigurationData
    {
        public int SampleRate { get; set; } = 0;
        public int BitsPerSample { get; set; } = 0;
        public AudioEncodingTypeEnum AudioEncodingType { get; set; } = AudioEncodingTypeEnum.PCM;
    }
}
