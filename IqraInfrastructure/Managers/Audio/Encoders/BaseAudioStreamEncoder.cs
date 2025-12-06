using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.TTS;
using IqraCore.Interfaces.Audio;
using IqraInfrastructure.Helpers.Audio;

namespace IqraInfrastructure.Managers.Audio.Encoders
{
    public abstract class BaseAudioStreamEncoder : IAudioStreamEncoder
    {
        public AudioEncodingTypeEnum TargetEncoding { get; }
        public int TargetSampleRate { get; }
        public int TargetBitsPerSample { get; }

        protected BaseAudioStreamEncoder(AudioEncodingTypeEnum encoding, int sampleRate, int bitsPerSample)
        {
            TargetEncoding = encoding;
            TargetSampleRate = sampleRate;
            TargetBitsPerSample = bitsPerSample;
        }

        public abstract byte[] Encode(ReadOnlySpan<byte> pcmData, int inputSampleRate, int inputBitsPerSample);

        public abstract void Dispose();

        /// <summary>
        /// Helper to convert incoming PCM to the specific PCM format required by the Codec.
        /// Example: Master is 24kHz/16bit -> Codec needs 8kHz/16bit.
        /// </summary>
        protected byte[] ResampleAndFormat(ReadOnlySpan<byte> inputData, int inputRate, int inputBits, int requiredRate, int requiredBits)
        {
            // Optimization: Pass through if already matching
            if (inputRate == requiredRate && inputBits == requiredBits)
            {
                return inputData.ToArray();
            }

            var sourceFormat = new TTSProviderAvailableAudioFormat
            {
                Encoding = AudioEncodingTypeEnum.PCM,
                SampleRateHz = inputRate,
                BitsPerSample = inputBits
            };

            var targetReq = new AudioRequestDetails
            {
                RequestedEncoding = AudioEncodingTypeEnum.PCM,
                RequestedSampleRateHz = requiredRate,
                RequestedBitsPerSample = requiredBits
            };

            // We use the existing helper. In a future optimization phase, 
            // we can make this stateful (keeping the Resampler instance) to reduce allocation.
            var (converted, _) = AudioConversationHelper.Convert(inputData.ToArray(), sourceFormat, targetReq, false);
            return converted;
        }
    }
}
