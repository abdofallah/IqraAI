using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.TTS;
using IqraInfrastructure.Helpers.Audio;
using IqraInfrastructure.Managers.Conversation.Session.Agent.AI;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.TurnEnd
{
    /// <summary>
    /// A service that uses the Pipecat Smart Turn ONNX model to perform
    /// intelligent, ML-based turn-end detection on a complete audio utterance.
    /// </summary>
    public class SmartTurnService : IDisposable
    {
        public event Action? TurnEnded;

        private readonly ILogger<SmartTurnService> _logger;
        private readonly SmartTurnOnnxModel _model;
        private readonly ConversationAIAgentState _agentState;

        public SmartTurnService(ILoggerFactory loggerFactory, ConversationAIAgentState agentState)
        {
            _logger = loggerFactory.CreateLogger<SmartTurnService>();
            _agentState = agentState;
            // The model path should be managed via configuration in a real app,
            // but hardcoding is fine for this implementation.
            var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "SmartTurn", "smart-turn-v3.0.onnx");
            _model = new SmartTurnOnnxModel(modelPath);
        }

        /// <summary>
        /// Analyzes a complete audio utterance to determine if the turn is complete.
        /// This is a "one-shot" analysis and should be triggered by VAD silence.
        /// </summary>
        /// <param name="turnAudioData">The raw audio bytes of the user's full turn.</param>
        public void AnalyzeTurn(byte[] turnAudioData)
        {
            if (turnAudioData == null || turnAudioData.Length == 0)
            {
                return;
            }

            try
            {
                // 1. Convert the raw turn audio (likely mulaw/pcm) to the required 16kHz, 32-bit float format.
                var audioFloats = ConvertTo16kHzFloat(turnAudioData);

                // 2. Run the prediction using the ONNX model wrapper.
                var (isComplete, probability) = _model.Predict(audioFloats);

                _logger.LogDebug("Smart Turn analysis complete. Prediction: {Prediction}, Probability: {Probability:F4}", isComplete ? "COMPLETE" : "INCOMPLETE", probability);

                // 3. If the model predicts the turn is complete, fire the event.
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
                    Encoding = _agentState.AgentConfiguration.AudioEncodingType,
                    SampleRateHz = _agentState.AgentConfiguration.SampleRate,
                    BitsPerSample = _agentState.AgentConfiguration.BitsPerSample
                }
            );

            var resampler = AudioConversationHelper.CreateResampler(
                sourceProvider,
                new AudioRequestDetails
                {
                    RequestedEncoding = AudioEncodingTypeEnum.PCM,
                    RequestedSampleRateHz = 16000,
                    RequestedBitsPerSample = 32 // float
                }
            );

            // Read all samples from the resampler into a list and then convert to an array.
            var samples = new List<float>();
            var buffer = new float[1024];
            int bytesRead;
            while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
            {
                samples.AddRange(buffer.Take(bytesRead));
            }
            return samples.ToArray();
        }

        public void Dispose()
        {
            _model?.Dispose();
        }
    }

}
