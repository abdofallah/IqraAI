using IqraCore.Entities.Helper.Audio;

namespace IqraCore.Entities.TTS
{
    public class TTSProviderAvailableAudioFormat
    {
        public AudioEncodingTypeEnum Encoding { get; set; }
        public int SampleRateHz { get; set; }
        public int BitsPerSample { get; set; }

        public override string ToString()
        {
            return $"{Encoding} @ {SampleRateHz / 1000}kHz, {BitsPerSample}-bit";
        }
    }
}
