using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.FishAudio
{
    public class FishAudioConfig : ITTSConfig
    {
        public int ConfigVersion => 1;
        public string ReferenceId { get; set; }
        public string Model { get; set; }
        public int TargetSampleRate { get; set; }
        public int TargetBitsPerSample { get; set; }
        public AudioEncodingTypeEnum TargetEncodingType { get; set; }
    }
}
