using IqraInfrastructure.Helpers.HuggingFace;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace IqraInfrastructure.Managers.TurnEnd
{
    /// <summary>
    /// Manages the ONNX inference session for the Smart Turn model.
    /// It handles audio pre-processing, running the model, and interpreting the output.
    /// </summary>
    public class SmartTurnOnnxModel
    {
        private static readonly string ModelFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "SmartTurn");
        private static readonly string ModelFileName = "smart-turn-v3.2-cpu.onnx";
        private static readonly string ModelPath = Path.Combine(ModelFolder, ModelFileName);

        private static InferenceSession Session;
        private static WhisperFeatureExtractor FeatureExtractor;

        private static bool IsModelLoaded = false;
        private static readonly object _initLock = new object();

        public SmartTurnOnnxModel()
        {
            if (!IsModelLoaded)
            {
                lock (_initLock)
                {
                    if (!IsModelLoaded)
                    {
                        var sessionOptions = new SessionOptions
                        {
                            ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
                            InterOpNumThreads = 1,
                            IntraOpNumThreads = 1,
                            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
                        };

                        Session = new InferenceSession(ModelPath, sessionOptions);
                        FeatureExtractor = new WhisperFeatureExtractor(8);

                        IsModelLoaded = true;
                    }
                }
            }
        }

        public static void EnsureModelExists()
        {
            if (!File.Exists(ModelPath))
            {
                Task.Run(async () =>
                {
                    await HFDownloader.DownloadModelAsync(
                        repoId: "pipecat-ai/smart-turn-v3",
                        filesToDownload: new List<string> { ModelFileName },
                        outputFolder: ModelFolder,
                        replaceIfShaMismatch: true
                    );
                }).GetAwaiter().GetResult();
            }
        }

        public (bool isComplete, float probability) Predict(float[] audio_16khz_mono)
        {
            float[] processedAudio = ProcessAudioLength(audio_16khz_mono);

            float[,] featureMatrix = FeatureExtractor.Process(processedAudio);

            var flatFeatures = featureMatrix.Cast<float>().ToArray();

            var dimensions = new int[] { 1, featureMatrix.GetLength(0), featureMatrix.GetLength(1) };
            var inputTensor = new DenseTensor<float>(flatFeatures.AsMemory(), dimensions);

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_features", inputTensor)
            };

            using (var outputs = Session.Run(inputs))
            {
                var resultTensor = outputs.First().AsTensor<float>();
                float probability = resultTensor.GetValue(0);
                bool isComplete = probability > 0.5f;
                return (isComplete, probability);
            }
        }

        private float[] ProcessAudioLength(float[] audio)
        {
            const int targetSamples = 8 * 16000;
            if (audio.Length == targetSamples) return audio;

            if (audio.Length > targetSamples)
            {
                var segment = new ArraySegment<float>(audio, audio.Length - targetSamples, targetSamples);
                return segment.ToArray();
            }
            else
            {
                int padding = targetSamples - audio.Length;
                var paddedAudio = new float[targetSamples];
                Array.Copy(audio, 0, paddedAudio, padding, audio.Length);
                return paddedAudio;
            }
        }
    }
}
