using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.TTS;
using IqraCore.Interfaces.Audio.Decoders;
using IqraInfrastructure.Helpers.Audio;

namespace IqraInfrastructure.Managers.Audio.Decoders
{
    public abstract class BaseAudioStreamDecoder : IAudioStreamDecoder
    {
        public AudioEncodingTypeEnum InputEncoding { get; }
        public int InputSampleRate { get; }
        public int InputBitsPerSample { get; }

        // The Enforced System Standard for Internal Processing
        protected const int INTERNAL_SAMPLE_RATE = 16000;
        protected const int INTERNAL_BITS_PER_SAMPLE = 32;

        protected BaseAudioStreamDecoder(AudioEncodingTypeEnum encoding, int sampleRate, int bitsPerSample)
        {
            InputEncoding = encoding;
            InputSampleRate = sampleRate;
            InputBitsPerSample = bitsPerSample;
        }

        public abstract byte[] Decode(ReadOnlySpan<byte> inputData);

        public abstract void Dispose();

        /// <summary>
        /// Helper to convert intermediate PCM (e.g., 8kHz 16-bit from MuLaw) 
        /// to the System Standard (16kHz 32-bit).
        /// </summary>
        protected byte[] ConvertToSystemStandard(byte[] intermediatePcmData, int intermediateRate, int intermediateBits)
        {
            // Optimization: If already matches standard, return as is
            if (intermediateRate == INTERNAL_SAMPLE_RATE && intermediateBits == INTERNAL_BITS_PER_SAMPLE)
            {
                return intermediatePcmData;
            }

            var sourceFormat = new TTSProviderAvailableAudioFormat
            {
                Encoding = AudioEncodingTypeEnum.PCM,
                SampleRateHz = intermediateRate,
                BitsPerSample = intermediateBits
            };

            var targetReq = new AudioRequestDetails
            {
                RequestedEncoding = AudioEncodingTypeEnum.PCM,
                RequestedSampleRateHz = INTERNAL_SAMPLE_RATE,
                RequestedBitsPerSample = INTERNAL_BITS_PER_SAMPLE
            };

            // Reuse the robust AudioConversationHelper
            var (converted, _) = AudioConversationHelper.Convert(intermediatePcmData, sourceFormat, targetReq, false);
            return converted;
        }
    }
}
