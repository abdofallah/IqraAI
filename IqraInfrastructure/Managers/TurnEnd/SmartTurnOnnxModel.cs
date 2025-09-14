using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace IqraInfrastructure.Managers.TurnEnd
{
    /// <summary>
    /// Manages the ONNX inference session for the Smart Turn model.
    /// It handles audio pre-processing, running the model, and interpreting the output.
    /// </summary>
    public class SmartTurnOnnxModel : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly WhisperFeatureExtractor _featureExtractor;

        public SmartTurnOnnxModel(string modelPath)
        {
            // It's good practice to use session options for performance tuning.
            var sessionOptions = new SessionOptions
            {
                ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
                InterOpNumThreads = 1,
                IntraOpNumThreads = 1,
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
            };
            _session = new InferenceSession(modelPath, sessionOptions);
            _featureExtractor = new WhisperFeatureExtractor();
        }

        /// <summary>
        /// Predicts if a given audio utterance is a complete turn.
        /// </summary>
        /// <param name="audio_16khz_mono">The audio waveform at 16kHz.</param>
        /// <returns>A tuple containing a boolean (isComplete) and the raw probability.</returns>
        public (bool isComplete, float probability) Predict(float[] audio_16khz_mono)
        {
            // 1. Truncate or Pad audio to exactly 8 seconds.
            float[] processedAudio = ProcessAudioLength(audio_16khz_mono);

            // 2. Generate the log-Mel spectrogram features.
            float[,] featureMatrix = _featureExtractor.Process(processedAudio, audio_16khz_mono.Length);

            // 3. Convert the 2D feature matrix into a flat array for the Tensor.
            var flatFeatures = featureMatrix.Cast<float>().ToArray();

            // 4a. Define the shape for the tensor using 'int', not 'long'.
            var dimensions = new int[] { 1, featureMatrix.GetLength(0), featureMatrix.GetLength(1) };
            // 4b. Create the DenseTensor using the correct constructor overload.
            // We provide the data as a Memory<float> and the dimensions as a ReadOnlySpan<int>.
            var inputTensor = new DenseTensor<float>(flatFeatures.AsMemory(), dimensions);

            // 5. Prepare the input for the ONNX model. The input name must match the model's expected name.
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_features", inputTensor)
            };

            // 6. Run inference and get the output.
            using (var outputs = _session.Run(inputs))
            {
                var resultTensor = outputs.First().AsTensor<float>();
                float probability = resultTensor.GetValue(0); // The model outputs a single float probability
                bool isComplete = probability > 0.5f; // Use a 0.5 threshold for the binary decision
                return (isComplete, probability);
            }
        }

        /// <summary>
        /// Ensures the audio array is exactly 8 seconds long by padding or truncating.
        /// </summary>
        private float[] ProcessAudioLength(float[] audio)
        {
            const int targetSamples = 8 * 16000; // 128,000 samples
            if (audio.Length == targetSamples) return audio;

            if (audio.Length > targetSamples)
            {
                // Return only the last 8 seconds of audio.
                var segment = new ArraySegment<float>(audio, audio.Length - targetSamples, targetSamples);
                return segment.ToArray();
            }
            else
            {
                // Pad with zeros (silence) at the beginning to reach 8 seconds.
                int padding = targetSamples - audio.Length;
                var paddedAudio = new float[targetSamples];
                Array.Copy(audio, 0, paddedAudio, padding, audio.Length);
                return paddedAudio;
            }
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}
