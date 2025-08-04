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
        private static string BasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "BlandAIVM");
        private static string ModelPath = Path.Combine(BasePath, "voicemail_detector.onnx");
        private static string ModelConfigPath = Path.Combine(BasePath, "config.json");

        private static InferenceSession Session;
        private static IReadOnlyDictionary<long, string> Id2label;
        private static bool IsModelLoaded = false;
        private static bool IsLoadingModel = false;

        public BlandAIOnnxVoicemailDetectModel()
        {
            if (!IsModelLoaded)
            {
                while (IsLoadingModel)
                {
                    Task.Delay(100).GetAwaiter().GetResult();
                }

                try
                {
                    IsLoadingModel = true;

                    if (!File.Exists(ModelPath))
                        throw new FileNotFoundException("ONNX model file not found.", ModelPath);
                    if (!File.Exists(ModelConfigPath))
                        throw new FileNotFoundException("Model config file not found.", ModelConfigPath);

                    Session = new InferenceSession(ModelPath);

                    var configJson = File.ReadAllText(ModelConfigPath);
                    var modelConfig = JsonSerializer.Deserialize<ModelConfig>(configJson);

                    Id2label = modelConfig.id2label.ToDictionary(kvp => long.Parse(kvp.Key), kvp => kvp.Value);

                    IsModelLoaded = true;
                }
                finally
                {
                    IsLoadingModel = false;
                }
            }
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

            var modelInputName = Session.InputNames.First();
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(modelInputName, inputTensor)
            };

            using var results = Session.Run(inputs);

            var outputTensor = results.First().AsTensor<float>();

            var probabilities = Softmax(outputTensor.ToArray());

            var maxConfidence = probabilities.Max();
            var maxIndex = probabilities.ToList().IndexOf(maxConfidence);

            var predictedLabel = Id2label[maxIndex];

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
            Session?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
