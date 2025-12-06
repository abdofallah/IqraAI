using IqraCore.Entities.Helper.Audio;

namespace IqraInfrastructure.Managers.Audio.Decoders
{
    public class PcmStreamDecoder : BaseAudioStreamDecoder
    {
        public PcmStreamDecoder(int inputSampleRate, int inputBitsPerSample)
            : base(AudioEncodingTypeEnum.PCM, inputSampleRate, inputBitsPerSample)
        {
        }

        public override byte[] Decode(ReadOnlySpan<byte> inputData)
        {
            if (inputData.IsEmpty) return Array.Empty<byte>();

            // The input IS PCM, just potentially the wrong rate/bits.
            // We feed it directly to the standardizer.
            return ConvertToSystemStandard(inputData.ToArray(), InputSampleRate, InputBitsPerSample);
        }

        public override void Dispose() { }
    }
}