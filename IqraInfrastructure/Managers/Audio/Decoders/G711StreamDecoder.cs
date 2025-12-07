using IqraCore.Entities.Helper.Audio;
using SIPSorcery.Media;
using SIPSorceryMedia.Abstractions;

namespace IqraInfrastructure.Managers.Audio.Decoders
{
    public class MulawStreamDecoder : BaseAudioStreamDecoder
    {
        private readonly AudioEncoder _audioEncoder;

        public MulawStreamDecoder() : base(AudioEncodingTypeEnum.MULAW, 8000, 8)
        {
            _audioEncoder = new AudioEncoder();
        }

        public override byte[] Decode(ReadOnlySpan<byte> inputData)
        {
            if (inputData.IsEmpty) return Array.Empty<byte>();

            // 1. Decode MuLaw bytes -> PCM Shorts (16-bit 8kHz)
            // SIPSorcery returns short[]
            short[] pcmShorts = _audioEncoder.DecodeAudio(inputData.ToArray(), new AudioFormat(AudioCodecsEnum.PCMU, 0, 8000, 1));

            // 2. Convert short[] to byte[]
            // Since it's 16-bit PCM, we need 2 bytes per sample.
            byte[] pcmBytes = new byte[pcmShorts.Length * 2];
            Buffer.BlockCopy(pcmShorts, 0, pcmBytes, 0, pcmBytes.Length);

            // 3. Convert to System Standard (16kHz 32-bit)
            // We pass 8000Hz and 16-bit because that is what SIPSorcery decoded it to.
            return ConvertToSystemStandard(pcmBytes, 8000, 16);
        }

        public override void Dispose() { }
    }

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

        public override void Dispose() { }
    }
}
