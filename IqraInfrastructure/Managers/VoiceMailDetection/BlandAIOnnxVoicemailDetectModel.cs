using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Text.Json;

namespace IqraInfrastructure.Managers.VoiceMailDetection
{
    public class ModelConfig
    {
        public Dictionary<string, string> id2label { get; set; }
    }

    public class BlandAIOnnxVoicemailDetectModel : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly IReadOnlyDictionary<long, string> _id2label;

        public BlandAIOnnxVoicemailDetectModel(string modelPath, string configPath)
        {
            if (!File.Exists(modelPath))
                throw new FileNotFoundException("ONNX model file not found.", modelPath);
            if (!File.Exists(configPath))
                throw new FileNotFoundException("Model config file not found.", configPath);

            _session = new InferenceSession(modelPath);

            var configJson = File.ReadAllText(configPath);
            var modelConfig = JsonSerializer.Deserialize<ModelConfig>(configJson);

            _id2label = modelConfig.id2label.ToDictionary(kvp => long.Parse(kvp.Key), kvp => kvp.Value);
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

            using var results = _session.Run(inputs);

            var outputTensor = results.First().AsTensor<float>();

            var probabilities = Softmax(outputTensor.ToArray());

            var maxConfidence = probabilities.Max();
            var maxIndex = probabilities.ToList().IndexOf(maxConfidence);

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

            var paddedAudio = new float[MaxLength];
            if (audioData.Length > MaxLength)
            {
                Array.Copy(audioData, paddedAudio, MaxLength);
            }
            else
            {
                Array.Copy(audioData, paddedAudio, audioData.Length);
            }

            if (paddedAudio.Length == 0)
            {
                return paddedAudio;
            }

            var mean = paddedAudio.Average();

            var variance = paddedAudio.Select(v => (v - mean) * (v - mean)).Average();

            const float epsilon = 1e-7f;

            var stdDev = (float)Math.Sqrt(variance + epsilon);

            if (stdDev < 1e-9)
            {
                return new float[MaxLength];
            }

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
