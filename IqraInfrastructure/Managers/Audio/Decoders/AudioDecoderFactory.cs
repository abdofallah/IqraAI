using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.Audio.Decoders;

namespace IqraInfrastructure.Managers.Audio.Decoders
{
    public static class AudioDecoderFactory
    {
        public static IAudioStreamDecoder CreateDecoder(AudioEncodingTypeEnum type, int inputSampleRate, int inputBitsPerSample)
        {
            return type switch
            {
                AudioEncodingTypeEnum.PCM => new PcmStreamDecoder(inputSampleRate, inputBitsPerSample),
                AudioEncodingTypeEnum.WAV => new PcmStreamDecoder(inputSampleRate, inputBitsPerSample),
                AudioEncodingTypeEnum.MULAW => new MulawStreamDecoder(),
                AudioEncodingTypeEnum.ALAW => new AlawStreamDecoder(),
                AudioEncodingTypeEnum.OPUS => new OpusStreamDecoder(inputSampleRate),
                // G.722, G.729 can be added here following the G.711 pattern
                _ => throw new NotSupportedException($"No decoder found for {type}")
            };
        }
    }
}