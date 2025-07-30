using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.TTS;
using IqraCore.Interfaces.VAD;
using IqraInfrastructure.Helpers.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Buffers;
using System.Runtime.InteropServices;

namespace IqraInfrastructure.Managers.VAD // Or your preferred namespace
{
    public class SileroVadService : IVadService
    {
        private static string ModelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VadModels\\silero_vad.onnx");
        private static InferenceSession? InferenceSession = null;

        private static int SileroVadSampleRate = 16000;
        private static int Silero16khzWindowSizeSamples = 512;

        private readonly ILogger<SileroVadService> _logger;
        private VadOptions _options = new();

        // --- ADJUSTED FOR YOUR MODEL ---
        // Model state (Combined state tensor)
        private Tensor<float>? _state; // Single state tensor [2, 1, 128] expected
        private Tensor<long>? _srTensor; // Sample rate tensor

        // VAD detection state
        private bool _isCurrentlySpeaking = false;
        private bool _triggered = false;
        private float _speechProbability = 0.0f;
        private int _speechDurationSamples = 0;
        private int _silenceDurationSamples = 0;
        private int _tempEndSamples = 0;

        // Configuration derived from options
        private int _minSilenceSamples = 0;
        private int _minSpeechSamples = 0;
        private int _speechPadSamples = 0;
        private int _windowSizeBytes = 0;

        // Model Constants (Match reference code/model)
        private const int HiddenSize = 128; // Based on reference code
        private const int NumLayersDirections = 2; // Based on reference code shape [2, 1, 128]
        private const int BatchSize = 1; // Based on reference code shape [2, 1, 128]

        // Audio buffering for windowing
        private byte[] _buffer;
        private int _bufferedBytes = 0;

        public event EventHandler<VadEventArgs>? VoiceActivityChanged;

        public SileroVadService(ILogger<SileroVadService> logger)
        {
            _logger = logger;

            if (InferenceSession == null)
            {
                if (!File.Exists(ModelPath))
                {
                    _logger.LogError("VAD model file not found at path: {ModelPath}", ModelPath);
                    throw new FileNotFoundException("VAD model file not found.", ModelPath);
                }

                var sessionOptions = new SessionOptions
                {
                    InterOpNumThreads = 1,
                    IntraOpNumThreads = 1,
                    EnableCpuMemArena = true,
                    GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
                };

                InferenceSession = new InferenceSession(ModelPath, sessionOptions);

                _logger.LogInformation("Silero VAD ONNX model loaded successfully from {ModelPath}", ModelPath);
            }

            _buffer = ArrayPool<byte>.Shared.Rent(1);
        }

        public void Initialize(VadOptions options)
        {
            _options = options;

            try
            {
                _windowSizeBytes = Silero16khzWindowSizeSamples * 2; // 16-bit PCM
                                                           // Use provided window size if explicitly set and valid
                if (_options.WindowSizeSamples > 0 && (_options.WindowSizeSamples == 512 || _options.WindowSizeSamples == 256))
                {
                    Silero16khzWindowSizeSamples = _options.WindowSizeSamples;
                    _windowSizeBytes = Silero16khzWindowSizeSamples * 2;
                }
                else if (_options.WindowSizeSamples > 0)
                {
                    _logger.LogWarning("Ignoring provided WindowSizeSamples ({ProvidedSize}), using default ({CalculatedSize}) based on sample rate.", _options.WindowSizeSamples, Silero16khzWindowSizeSamples);
                }
                // --- End Model Specific Config ---

                _minSilenceSamples = (SileroVadSampleRate * _options.MinSilenceDurationMs) / 1000;
                _minSpeechSamples = (SileroVadSampleRate * _options.MinSpeechDurationMs) / 1000;
                _speechPadSamples = (SileroVadSampleRate * _options.SpeechPadMs) / 1000;

                // Resize buffer based on final window size
                ArrayPool<byte>.Shared.Return(_buffer); // Return initial small buffer
                _buffer = ArrayPool<byte>.Shared.Rent(_windowSizeBytes);

                _logger.LogInformation("VAD Initialized: SampleRate={SampleRate}, WindowSamples={WindowSamples}, MinSilenceMs={MinSilenceMs}, MinSpeechMs={MinSpeechMs}, PaddingMs={SpeechPadMs}, Threshold={Threshold}",
                    SileroVadSampleRate, Silero16khzWindowSizeSamples, _options.MinSilenceDurationMs, _options.MinSpeechDurationMs, _options.SpeechPadMs, _options.Threshold);

                Reset(); // Initialize state variables and model hidden states
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Silero VAD service.");
                Dispose();
                throw;
            }
        }

        public void ProcessAudio(byte[] audioChunkData)
        {
            if (InferenceSession == null) return;
            if (audioChunkData.Length == 0) return;

            var convertedAudio = AudioConversationHelper.Convert(
                audioChunkData,
                new TTSProviderAvailableAudioFormat()
                { 
                    Encoding = _options.AudioEncodingType,
                    SampleRateHz = _options.SampleRate,
                    BitsPerSample = _options.BitsPerSample
                },
                new AudioRequestDetails()
                { 
                    RequestedEncoding = AudioEncodingTypeEnum.PCM,
                    RequestedSampleRateHz = 16000,
                    RequestedBitsPerSample = 32
                },
                false
            );

            var pcm16AudioChunk = convertedAudio.audioData.AsMemory();
            if (pcm16AudioChunk.IsEmpty) return;

            int bytesProcessed = 0;
            while (bytesProcessed < pcm16AudioChunk.Length)
            {
                int bytesToCopy = Math.Min(pcm16AudioChunk.Length - bytesProcessed, _windowSizeBytes - _bufferedBytes);
                pcm16AudioChunk.Span.Slice(bytesProcessed, bytesToCopy).CopyTo(_buffer.AsSpan().Slice(_bufferedBytes));
                _bufferedBytes += bytesToCopy;
                bytesProcessed += bytesToCopy;

                if (_bufferedBytes == _windowSizeBytes)
                {
                    ProcessWindow(_buffer.AsMemory().Slice(0, _windowSizeBytes));
                    _bufferedBytes = 0;
                }
            }
        }

        private void ProcessWindow(ReadOnlyMemory<byte> windowData)
        {
            // Use correct model input/output names
            const string AudioInputName = "input";
            const string SrInputName = "sr";
            const string StateInputName = "state";
            const string ProbOutputName = "output";
            const string StateOutputName = "stateN";

            // Check if initialized properly
            if (InferenceSession == null || _state == null || _srTensor == null) return;

            try
            {
                // 1. Convert PCM16khz32bit bytes to float32 tensor
                var floatData = MemoryMarshal.Cast<byte, float>(windowData.Span);
                var inputTensor = new DenseTensor<float>(new[] { BatchSize, Silero16khzWindowSizeSamples });
                for (int i = 0; i < Math.Min(floatData.Length, Silero16khzWindowSizeSamples); i++)
                {
                    inputTensor.SetValue(i, floatData[i]);
                }

                // 2. Prepare inputs using correct names and tensors
                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor(AudioInputName, inputTensor),
                    NamedOnnxValue.CreateFromTensor(SrInputName, _srTensor), // Tensor<long>
                    NamedOnnxValue.CreateFromTensor(StateInputName, _state)   // Tensor<float> [2, 1, 128]
                };

                // 3. Run Inference
                using var results = InferenceSession.Run(inputs);

                // 4. Extract outputs using correct names
                var outputTensor = results.FirstOrDefault(r => r.Name == ProbOutputName)?.AsTensor<float>();
                // Update the single state tensor
                _state = results.FirstOrDefault(r => r.Name == StateOutputName)?.AsTensor<float>() ?? _state;

                if (outputTensor == null)
                {
                    _logger.LogWarning("VAD model did not return '{ProbOutputName}' tensor.", ProbOutputName);
                    return;
                }

                // Check output shape - Expected: [batch_size, 1] -> [1, 1]
                if (outputTensor.Rank != 2 || outputTensor.Dimensions[0] != BatchSize || outputTensor.Dimensions[1] != 1)
                {
                    _logger.LogWarning("Unexpected output tensor shape received: [{Shape}]", string.Join(',', outputTensor.Dimensions.ToArray()));
                    // Attempt to get the first value anyway if possible
                    _speechProbability = outputTensor.GetValue(0);
                }
                else
                {
                    _speechProbability = outputTensor[0, 0]; // Access using [batch_index, value_index] for shape [1,1]
                }


                // 5. Update VAD state machine
                UpdateVadState();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during VAD inference or state update.");
            }
        }

        private void UpdateVadState()
        {
            // This state machine logic should remain largely the same
            if (_speechProbability >= _options.Threshold)
            {
                _tempEndSamples = 0;
                _speechDurationSamples += Silero16khzWindowSizeSamples;

                if (_speechDurationSamples >= _minSpeechSamples)
                {
                    _triggered = true;
                    if (!_isCurrentlySpeaking)
                    {
                        _isCurrentlySpeaking = true;
                        OnVoiceActivityChanged(true);   
                    }
                    _silenceDurationSamples = 0;
                }
            }
            else // Below threshold
            {
                _silenceDurationSamples += Silero16khzWindowSizeSamples;

                if (_triggered && _silenceDurationSamples > _speechPadSamples)
                {
                    _tempEndSamples = _silenceDurationSamples;
                }

                int effectiveSilenceDuration = _tempEndSamples > 0 ? _tempEndSamples : _silenceDurationSamples;

                if (_isCurrentlySpeaking && effectiveSilenceDuration >= _minSilenceSamples)
                {
                    _isCurrentlySpeaking = false;
                    _triggered = false;
                    _tempEndSamples = 0;
                    OnVoiceActivityChanged(false);
                    _speechDurationSamples = 0;
                }
            }
        }

        public void Reset()
        {
            _logger.LogDebug("Resetting VAD state.");
            _isCurrentlySpeaking = false;
            _triggered = false;
            _speechProbability = 0.0f;
            _speechDurationSamples = 0;
            _silenceDurationSamples = 0;
            _tempEndSamples = 0;
            _bufferedBytes = 0;

            // Reset the single state tensor to zeros with correct shape [2, 1, 128]
            _state = new DenseTensor<float>(new[] { NumLayersDirections, BatchSize, HiddenSize });

            // Reset the sample rate tensor
            _srTensor = new DenseTensor<long>(new long[] { SileroVadSampleRate }, new[] { 1 }); // Shape [1]

            Array.Clear(_buffer, 0, _buffer.Length);
        }

        private void OnVoiceActivityChanged(bool isSpeechDetected)
        {
            try { VoiceActivityChanged?.Invoke(this, new VadEventArgs(isSpeechDetected)); }
            catch (Exception ex) { _logger.LogError(ex, "Error invoking VoiceActivityChanged event handler."); }
        }

        public void Dispose()
        {
            _logger.LogInformation("Disposing SileroVadService.");
            InferenceSession?.Dispose();
            InferenceSession = null;
            ArrayPool<byte>.Shared.Return(_buffer);
            GC.SuppressFinalize(this);
        }
    }

}