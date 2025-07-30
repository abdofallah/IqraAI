using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Text.Json;

namespace IqraInfrastructure.Managers.VoiceMailDetection
{
    public class ModelConfig
    {
        // We only care about the id2label part of the config
        public Dictionary<string, string> id2label { get; set; }
    }

    public class OnnxVoiceMailDetector : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly IReadOnlyDictionary<long, string> _id2label;

        // Constants derived from the Python code and model properties
        private const int TargetSamplingRate = 16000;

        public OnnxVoiceMailDetector(string modelPath, string configPath)
        {
            if (!File.Exists(modelPath))
                throw new FileNotFoundException("ONNX model file not found.", modelPath);
            if (!File.Exists(configPath))
                throw new FileNotFoundException("Model config file not found.", configPath);

            // 1. Load the ONNX model into an InferenceSession
            // You can add SessionOptions here if needed, similar to your VAD service
            _session = new InferenceSession(modelPath);
            Console.WriteLine($"ONNX model loaded successfully from '{modelPath}'.");

            // 2. Load and parse the configuration to get the id2label mapping
            var configJson = File.ReadAllText(configPath);
            var modelConfig = JsonSerializer.Deserialize<ModelConfig>(configJson);

            // Convert the dictionary from <string, string> to <long, string> for easier lookup
            _id2label = modelConfig.id2label
                .ToDictionary(kvp => long.Parse(kvp.Key), kvp => kvp.Value);

            Console.WriteLine("Model configuration loaded.");
        }

        public (string Label, float Confidence) Predict(float[] audioSource)
        {
            if (audioSource == null || audioSource.Length == 0)
            {
                throw new ArgumentException("Audio source cannot be null or empty.");
            }

            var processedAudio = PreprocessAudio(audioSource);

            var dimensions = new int[] { 1, processedAudio.Length };

            var inputTensor = new DenseTensor<float>(new Memory<float>(processedAudio), dimensions);

            var modelInputName = _session.InputNames.First();
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(modelInputName, inputTensor)
            };

            // 3. Run inference
            using var results = _session.Run(inputs);

            // 4. Get the output logits
            var outputTensor = results.First().AsTensor<float>();

            // 5. Apply Softmax to convert logits to probabilities
            var probabilities = Softmax(outputTensor.ToArray());

            // 6. Find the highest probability and its index
            var maxConfidence = probabilities.Max();
            var maxIndex = probabilities.ToList().IndexOf(maxConfidence);

            // 7. Map the index to the label from our config
            var predictedLabel = _id2label[maxIndex];

            return (predictedLabel, maxConfidence);
        }

        private float[] PreprocessAudio(float[] audioData)
        {
            int MaxLength = audioData.Length;
            if (MaxLength < 512)
            {
                MaxLength = 512;
            }

            // --- Step 1: Pad or Truncate the audio to the model's required length (32,000 samples) ---
            // The normalization must be done on the final-sized array.
            var paddedAudio = new float[MaxLength]; // Initializes with zeros by default

            if (audioData.Length > MaxLength)
            {
                // Truncate
                Array.Copy(audioData, paddedAudio, MaxLength);
            }
            else
            {
                // Pad
                Array.Copy(audioData, paddedAudio, audioData.Length);
            }

            // --- Step 2: Perform Zero-Mean, Unit-Variance Normalization ---
            // This replicates the behavior of `feature_extractor.zero_mean_unit_var_norm`

            if (paddedAudio.Length == 0)
            {
                return paddedAudio; // Return empty if input is empty
            }

            // Calculate the mean (average)
            var mean = paddedAudio.Average();

            // Calculate the variance
            var variance = paddedAudio.Select(v => (v - mean) * (v - mean)).Average();

            // The small epsilon value for numerical stability, just like in the Python code (1e-7)
            const float epsilon = 1e-7f;

            // Calculate the standard deviation from variance
            var stdDev = (float)Math.Sqrt(variance + epsilon);

            // If standard deviation is effectively zero, the result is all zeros
            if (stdDev < 1e-9) // A small threshold to handle silent audio
            {
                return new float[MaxLength];
            }

            // Create the final normalized array
            var normalizedAudio = new float[MaxLength];
            for (int i = 0; i < paddedAudio.Length; i++)
            {
                normalizedAudio[i] = (paddedAudio[i] - mean) / stdDev;
            }

            return normalizedAudio;
        }

        private float[] Softmax(float[] logits)
        {
            var maxLogit = logits.Max();
            var exps = logits.Select(l => (float)Math.Exp(l - maxLogit));
            var sumExps = exps.Sum();
            return exps.Select(e => e / sumExps).ToArray();
        }

        public void Dispose()
        {
            _session?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
