using IqraCore.Entities.Helper.Audio;
using SIPSorcery.Media;
using SIPSorceryMedia.Abstractions;
using System;

namespace IqraInfrastructure.Managers.Audio.Decoders
{
    public class G729StreamDecoder : BaseAudioStreamDecoder
    {
        private readonly AudioEncoder _audioEncoder;

        // G.729 operates at 8kHz and decodes into 16-bit PCM
        public G729StreamDecoder() : base(AudioEncodingTypeEnum.G729, 8000, 16)
        {
            _audioEncoder = new AudioEncoder();
        }

        public override byte[] Decode(ReadOnlySpan<byte> inputData)
        {
            if (inputData.IsEmpty) return Array.Empty<byte>();

            // 1. Decode G729 bytes -> PCM Shorts (16-bit 8kHz)
            // Payload type 18 is standard for G.729
            short[] pcmShorts = _audioEncoder.DecodeAudio(inputData.ToArray(), new AudioFormat(AudioCodecsEnum.G729, 18, 8000, 1));

            // 2. Convert short[] to byte[]
            byte[] pcmBytes = new byte[pcmShorts.Length * 2];
            Buffer.BlockCopy(pcmShorts, 0, pcmBytes, 0, pcmBytes.Length);

            // 3. Convert to System Standard (16kHz 32-bit)
            // Passing 8000Hz and 16-bit as the intermediate state
            return ConvertToSystemStandard(pcmBytes, 8000, 16);
        }

        public override void Dispose()
        {
            _audioEncoder?.Dispose();
        }
    }
}