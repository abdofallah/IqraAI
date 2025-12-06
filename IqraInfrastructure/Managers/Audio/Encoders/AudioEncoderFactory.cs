using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.Audio;

namespace IqraInfrastructure.Managers.Audio.Encoders
{
    public static class AudioEncoderFactory
    {
        public static IAudioStreamEncoder CreateEncoder(AudioEncodingTypeEnum type, int sampleRate, int bitsPerSample, int frameDurationMs = 60)
        {
            return type switch
            {
                AudioEncodingTypeEnum.PCM => new PcmStreamEncoder(sampleRate, bitsPerSample),
                AudioEncodingTypeEnum.WAV => new PcmStreamEncoder(sampleRate, bitsPerSample),
                AudioEncodingTypeEnum.MULAW => new MulawStreamEncoder(),
                AudioEncodingTypeEnum.ALAW => new AlawStreamEncoder(),
                AudioEncodingTypeEnum.OPUS => new OpusStreamEncoder(sampleRate, frameDurationMs),
                // Add G722/MPEG later
                _ => throw new NotSupportedException($"No encoder found for {type}")
            };
        }
    }
}
