using IqraCore.Entities.Helper.Audio;
using SIPSorcery.Media;
using SIPSorceryMedia.Abstractions;
using System;

namespace IqraInfrastructure.Managers.Audio.Encoders
{
    public class G729StreamEncoder : BaseAudioStreamEncoder
    {
        private readonly AudioEncoder _audioEncoder;

        // G.729 expects 8kHz 16-bit PCM as its input
        public G729StreamEncoder() : base(AudioEncodingTypeEnum.G729, 8000, 16)
        {
            _audioEncoder = new AudioEncoder();
        }

        public override byte[] Encode(ReadOnlySpan<byte> pcmData, int inputSampleRate, int inputBitsPerSample)
        {
            if (pcmData.IsEmpty) return Array.Empty<byte>();

            // 1. G.729 requires 8kHz, 16-bit linear PCM input
            var pcm8k16bit = ResampleAndFormat(pcmData, inputSampleRate, inputBitsPerSample, 8000, 16);

            // 2. Convert bytes to shorts
            short[] pcmShorts = new short[pcm8k16bit.Length / 2];
            Buffer.BlockCopy(pcm8k16bit, 0, pcmShorts, 0, pcm8k16bit.Length);

            // 3. Encode using SIPSorcery
            // Payload type 18 is standard for G.729
            return _audioEncoder.EncodeAudio(pcmShorts, new AudioFormat(AudioCodecsEnum.G729, 18, 8000, 1));
        }

        public override void Dispose()
        {
            _audioEncoder?.Dispose();
        }
    }
}