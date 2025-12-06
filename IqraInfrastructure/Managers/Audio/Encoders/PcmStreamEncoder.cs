using IqraCore.Entities.Helper.Audio;

namespace IqraInfrastructure.Managers.Audio.Encoders
{
    public class PcmStreamEncoder : BaseAudioStreamEncoder
    {
        public PcmStreamEncoder(int targetSampleRate, int targetBitsPerSample)
            : base(AudioEncodingTypeEnum.PCM, targetSampleRate, targetBitsPerSample)
        {
        }

        public override byte[] Encode(ReadOnlySpan<byte> pcmData, int inputSampleRate, int inputBitsPerSample)
        {
            // Simply resample/re-quantize to the user's requested PCM format
            return ResampleAndFormat(pcmData, inputSampleRate, inputBitsPerSample, TargetSampleRate, TargetBitsPerSample);
        }

        public override void Dispose() { /* Nothing to dispose */ }
    }
}
