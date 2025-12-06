using IqraCore.Entities.Helper.Audio;
using SIPSorcery.Media;
using SIPSorceryMedia.Abstractions;

namespace IqraInfrastructure.Managers.Audio.Encoders
{
    public class MulawStreamEncoder : BaseAudioStreamEncoder
    {
        private readonly AudioEncoder _audioEncoder;

        public MulawStreamEncoder() : base(AudioEncodingTypeEnum.MULAW, 8000, 8)
        {
            _audioEncoder = new AudioEncoder();
        }

        public override byte[] Encode(ReadOnlySpan<byte> pcmData, int inputSampleRate, int inputBitsPerSample)
        {
            // 1. G.711 requires 8kHz, 16-bit linear PCM input
            var pcm8k16bit = ResampleAndFormat(pcmData, inputSampleRate, inputBitsPerSample, 8000, 16);

            // 2. Convert bytes to shorts
            short[] pcmShorts = new short[pcm8k16bit.Length / 2];
            Buffer.BlockCopy(pcm8k16bit, 0, pcmShorts, 0, pcm8k16bit.Length);

            // 3. Encode using SIPSorcery
            return _audioEncoder.EncodeAudio(pcmShorts, new AudioFormat(AudioCodecsEnum.PCMU, 0, 8000, 1));
        }

        public override void Dispose() { }
    }

    public class AlawStreamEncoder : BaseAudioStreamEncoder
    {
        private readonly AudioEncoder _audioEncoder;

        public AlawStreamEncoder() : base(AudioEncodingTypeEnum.ALAW, 8000, 8)
        {
            _audioEncoder = new AudioEncoder();
        }

        public override byte[] Encode(ReadOnlySpan<byte> pcmData, int inputSampleRate, int inputBitsPerSample)
        {
            // 1. G.711 requires 8kHz, 16-bit linear PCM input
            var pcm8k16bit = ResampleAndFormat(pcmData, inputSampleRate, inputBitsPerSample, 8000, 16);

            // 2. Convert bytes to shorts
            short[] pcmShorts = new short[pcm8k16bit.Length / 2];
            Buffer.BlockCopy(pcm8k16bit, 0, pcmShorts, 0, pcm8k16bit.Length);

            // 3. Encode using SIPSorcery
            return _audioEncoder.EncodeAudio(pcmShorts, new AudioFormat(AudioCodecsEnum.PCMA, 8, 8000, 1));
        }

        public override void Dispose() { }
    }
}
