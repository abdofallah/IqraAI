using IqraCore.Entities.Helper.Audio;
using SIPSorcery.Media;
using SIPSorceryMedia.Abstractions;

namespace IqraInfrastructure.Managers.Audio.Encoders
{
    public class G722StreamEncoder : BaseAudioStreamEncoder
    {
        private readonly AudioEncoder _audioEncoder;

        // G.722 outputs at 64kbps, but its input/working space is 16kHz 16-bit PCM
        public G722StreamEncoder() : base(AudioEncodingTypeEnum.G722, 16000, 16)
        {
            _audioEncoder = new AudioEncoder();
        }

        public override byte[] Encode(ReadOnlySpan<byte> pcmData, int inputSampleRate, int inputBitsPerSample)
        {
            if (pcmData.IsEmpty) return Array.Empty<byte>();

            // 1. G.722 requires 16kHz, 16-bit linear PCM input
            var pcm16k16bit = ResampleAndFormat(pcmData, inputSampleRate, inputBitsPerSample, 16000, 16);

            // 2. Convert bytes to shorts
            // Since it is 16-bit, 2 bytes = 1 short
            short[] pcmShorts = new short[pcm16k16bit.Length / 2];
            Buffer.BlockCopy(pcm16k16bit, 0, pcmShorts, 0, pcm16k16bit.Length);

            // 3. Encode using SIPSorcery
            // Payload type 9 is the standard static payload type for G.722
            return _audioEncoder.EncodeAudio(pcmShorts, new AudioFormat(AudioCodecsEnum.G722, 9, 16000, 1));
        }

        public override void Dispose()
        {
            _audioEncoder?.Dispose();
        }
    }
}