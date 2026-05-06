using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.TTS;
using IqraInfrastructure.Helpers.Audio;
using SIPSorcery.Media;

namespace IqraInfrastructure.Managers.Audio.Encoders
{
    public class MulawStreamEncoder : BaseAudioStreamEncoder
    {
        private readonly AudioEncoder _audioEncoder;

        public MulawStreamEncoder() : base(AudioEncodingTypeEnum.MULAW, 8000, 8)
        {
            _audioEncoder = new AudioEncoder();
        }

        public override byte[] Encode(ReadOnlySpan<byte> pcmData, int inputSampleRate, int inputBitsPerSample)
        {
            var convertedAudio = AudioConversationHelper.Convert(
                pcmData.ToArray(),
                new TTSProviderAvailableAudioFormat
                {
                    Encoding = AudioEncodingTypeEnum.PCM,
                    SampleRateHz = inputSampleRate,
                    BitsPerSample = inputBitsPerSample
                },
                new AudioRequestDetails
                {
                    RequestedEncoding = AudioEncodingTypeEnum.MULAW,
                    RequestedSampleRateHz = 8000,
                    RequestedBitsPerSample = 8
                },
                false
            );

            return convertedAudio.audioData;
        }

        public override void Dispose()
        {
            _audioEncoder?.Dispose();
        }
    }
}
