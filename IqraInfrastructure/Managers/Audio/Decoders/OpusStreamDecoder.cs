using Concentus;
using IqraCore.Entities.Helper.Audio;

namespace IqraInfrastructure.Managers.Audio.Decoders
{
    public class OpusStreamDecoder : BaseAudioStreamDecoder
    {
        private readonly IOpusDecoder _decoder;
        private readonly int _internalOpusRate = 48000;
        private readonly int _channels = 1;

        public OpusStreamDecoder(int inputSampleRate)
            : base(AudioEncodingTypeEnum.OPUS, inputSampleRate, 16) // Opus usually decodes to 16-bit
        {
            // We use 48kHz for the internal decoder as it's Opus's native rate
            _decoder = OpusCodecFactory.CreateDecoder(_internalOpusRate, _channels);
        }

        public override byte[] Decode(ReadOnlySpan<byte> inputData)
        {
            if (inputData.IsEmpty) return Array.Empty<byte>();

            try
            {
                // 1. Decode Opus Frame -> PCM Shorts (48kHz 16-bit)
                // Max frame size for 60ms at 48kHz is 2880 samples
                int maxFrameSize = _internalOpusRate * 120 / 1000; // ample buffer
                short[] pcmShorts = new short[maxFrameSize];

                // Decode
                int samplesDecoded = _decoder.Decode(inputData.ToArray(), pcmShorts, maxFrameSize, false);

                // Convert short[] to byte[]
                byte[] pcm48k16bit = new byte[samplesDecoded * 2];
                Buffer.BlockCopy(pcmShorts, 0, pcm48k16bit, 0, pcm48k16bit.Length);

                // 2. Convert to System Standard (16kHz 32-bit)
                return ConvertToSystemStandard(pcm48k16bit, _internalOpusRate, 16);
            }
            catch
            {
                // Packet loss or corruption. Return silence or empty to avoid crashing the session.
                return Array.Empty<byte>();
            }
        }

        public override void Dispose()
        {
            // Concentus is managed
        }
    }
}