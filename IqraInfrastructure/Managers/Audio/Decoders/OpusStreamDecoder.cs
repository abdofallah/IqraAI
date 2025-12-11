using Concentus;
using IqraCore.Entities.Helper.Audio;

namespace IqraInfrastructure.Managers.Audio.Decoders
{
    public class OpusStreamDecoder : BaseAudioStreamDecoder
    {
        private readonly IOpusDecoder _decoder;
        private readonly int _internalOpusRate;
        private readonly int _channels = 2;

        public OpusStreamDecoder(int inputSampleRate)
            : base(AudioEncodingTypeEnum.OPUS, inputSampleRate, 16) // Opus usually decodes to 16-bit
        {
            _internalOpusRate = inputSampleRate;

            _decoder = OpusCodecFactory.CreateDecoder(_internalOpusRate, _channels);
        }

        public override byte[] Decode(ReadOnlySpan<byte> inputData)
        {
            if (inputData.IsEmpty) return Array.Empty<byte>();

            try
            {
                // 1. Prepare Buffer
                // Max frame size for 120ms at 48kHz = 5760 samples per channel.
                int frameSizePerChannel = _internalOpusRate * 20 / 1000;
                int totalSamples = frameSizePerChannel * _channels;
                short[] pcmStereoShorts = new short[totalSamples];

                // 2. Decode (Produces Interleaved L-R-L-R...)
                int samplesDecodedPerChannel = _decoder.Decode(inputData.ToArray(), pcmStereoShorts, frameSizePerChannel, false);

                // 3. Downmix Stereo to Mono
                // We do this BEFORE converting to bytes to save memory allocation/copying.
                short[] pcmMonoShorts = new short[samplesDecodedPerChannel];

                // Efficient Downmix Loop
                // Mono = (Left + Right) / 2
                for (int i = 0; i < samplesDecodedPerChannel; i++)
                {
                    int left = pcmStereoShorts[i * 2];
                    int right = pcmStereoShorts[(i * 2) + 1];
                    pcmMonoShorts[i] = (short)((left + right) / 2);
                }

                // 4. Convert Mono Shorts to Bytes
                byte[] pcmMonoBytes = new byte[samplesDecodedPerChannel * 2];
                Buffer.BlockCopy(pcmMonoShorts, 0, pcmMonoBytes, 0, pcmMonoBytes.Length);

                // 5. Convert to System Standard (16kHz 32-bit)
                // We pass the mono bytes, telling the converter it is 16-bit at the decoded rate.
                return ConvertToSystemStandard(pcmMonoBytes, _internalOpusRate, 16);
            }
            catch
            {
                // Packet loss or corruption. Return silence.
                return Array.Empty<byte>();
            }
        }

        public override void Dispose()
        {
            // Concentus is managed
        }
    }
}