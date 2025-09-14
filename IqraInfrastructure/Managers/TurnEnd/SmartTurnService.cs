using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.TTS;
using IqraInfrastructure.Helpers.Audio;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.TurnEnd
{
    public class SmartTurnService
    {
        public event Action? TurnEnded;

        private readonly ILogger<SmartTurnService> _logger;
        private readonly SmartTurnOnnxModel _model;

        private readonly AudioEncodingTypeEnum _audioEncodingType;
        private readonly int _sampleRate;
        private readonly int _bitsPerSample;

        public SmartTurnService(ILogger<SmartTurnService> logger, AudioEncodingTypeEnum audioEncodingType, int sampleRate, int bitsPerSample)
        {
            _logger = logger;
            _model = new SmartTurnOnnxModel();

            _audioEncodingType = audioEncodingType;
            _sampleRate = sampleRate;
            _bitsPerSample = bitsPerSample;
        }

        public void AnalyzeTurn(byte[] turnAudioData)
        {
            if (turnAudioData == null || turnAudioData.Length == 0)
            {
                return;
            }

            try
            {
                var audioFloats = ConvertTo16kHzFloat(turnAudioData);

                AnalyzeTurn(audioFloats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Smart Turn analysis.");
                // We fail silently and don't fire the event, allowing fallback to other mechanisms if any.
            }
        }

        public void AnalyzeTurn(float[] turnAudioData)
        {
            if (turnAudioData == null || turnAudioData.Length == 0)
            {
                return;
            }

            try
            {
                var (isComplete, probability) = _model.Predict(turnAudioData);

                if (isComplete)
                {
                    TurnEnded?.Invoke();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Smart Turn analysis.");
                // We fail silently and don't fire the event, allowing fallback to other mechanisms if any.
            }
        }

        private float[] ConvertTo16kHzFloat(byte[] audioData)
        {
            var sourceProvider = AudioConversationHelper.CreatePcm32FloatProvider(
                audioData,
                new TTSProviderAvailableAudioFormat
                {
                    Encoding = _audioEncodingType,
                    SampleRateHz = _sampleRate,
                    BitsPerSample = _bitsPerSample
                }
            );

            var resampler = AudioConversationHelper.CreateResampler(
                sourceProvider,
                new AudioRequestDetails
                {
                    RequestedEncoding = AudioEncodingTypeEnum.PCM,
                    RequestedSampleRateHz = 16000,
                    RequestedBitsPerSample = 32
                }
            );

            var samples = new List<float>();
            var buffer = new float[1024];
            int bytesRead;
            while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
            {
                samples.AddRange(buffer.Take(bytesRead));
            }
            return samples.ToArray();
        }
    }

}
