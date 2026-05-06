using IqraCore.Entities.Helper.Audio;
using SIPSorcery.Media;
using SIPSorceryMedia.Abstractions;

namespace IqraInfrastructure.Managers.Audio.Decoders
{
    public class G722StreamDecoder : BaseAudioStreamDecoder
    {
        private readonly AudioEncoder _audioEncoder;

        public G722StreamDecoder() : base(AudioEncodingTypeEnum.G722, 16000, 14)
        {
            _audioEncoder = new AudioEncoder();
        }

        public override byte[] Decode(ReadOnlySpan<byte> inputData)
        {
            if (inputData.IsEmpty) return Array.Empty<byte>();

            // 1. Decode G722 bytes -> PCM Shorts (16-bit 16kHz)
            short[] pcmShorts = _audioEncoder.DecodeAudio(inputData.ToArray(), new AudioFormat(AudioCodecsEnum.G722, 9, 16000, 1));

            // 2. Convert short[] to byte[]
            // Since it's 16-bit PCM, we need 2 bytes per sample.
            byte[] pcmBytes = new byte[pcmShorts.Length * 2];
            Buffer.BlockCopy(pcmShorts, 0, pcmBytes, 0, pcmBytes.Length);

            // 3. Convert to System Standard (16kHz 32-bit)
            // SIPSorcery decoded it to 16000Hz and 16-bit.
            return ConvertToSystemStandard(pcmBytes, 16000, 16);
        }

        public override void Dispose()
        {
            _audioEncoder?.Dispose();
        }
    }
}
