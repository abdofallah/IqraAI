using IqraCore.Entities.Helper.Audio;
using SIPSorcery.Media;
using SIPSorceryMedia.Abstractions;

namespace IqraInfrastructure.Managers.Audio.Decoders
{
    public class AlawStreamDecoder : BaseAudioStreamDecoder
    {
        private readonly AudioEncoder _audioEncoder;

        public AlawStreamDecoder() : base(AudioEncodingTypeEnum.ALAW, 8000, 8)
        {
            _audioEncoder = new AudioEncoder();
        }

        public override byte[] Decode(ReadOnlySpan<byte> inputData)
        {
            if (inputData.IsEmpty) return Array.Empty<byte>();

            // 1. Decode ALaw bytes -> PCM Shorts (16-bit 8kHz)
            short[] pcmShorts = _audioEncoder.DecodeAudio(inputData.ToArray(), new AudioFormat(AudioCodecsEnum.PCMA, 8, 8000, 1));

            // 2. Convert short[] to byte[]
            byte[] pcmBytes = new byte[pcmShorts.Length * 2];
            Buffer.BlockCopy(pcmShorts, 0, pcmBytes, 0, pcmBytes.Length);

            // 3. Convert to System Standard (16kHz 32-bit)
            return ConvertToSystemStandard(pcmBytes, 8000, 16);
        }

        public override void Dispose()
        {
            _audioEncoder?.Dispose();
        }
    }
}
