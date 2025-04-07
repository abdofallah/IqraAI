using IqraCore.Interfaces.VAD;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace IqraInfrastructure.Managers.VAD // Or your preferred namespace
{
    public class SileroVadService : IVadService
    {
        private readonly ILogger<SileroVadService> _logger;
        private InferenceSession? _session;
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
        private int _windowSizeSamples = 0;

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
            // Buffer size calculation will happen in Initialize
            _buffer = ArrayPool<byte>.Shared.Rent(1); // Rent minimal initially
        }

        public void Initialize(string modelPath, VadOptions options)
        {
            if (!File.Exists(modelPath))
            {
                _logger.LogError("VAD model file not found at path: {ModelPath}", modelPath);
                throw new FileNotFoundException("VAD model file not found.", modelPath);
            }

            _options = options ?? new VadOptions();

            try
            {
                var sessionOptions = new SessionOptions
                {
                    // These seem standard for Silero VAD based on reference
                    InterOpNumThreads = 1,
                    IntraOpNumThreads = 1,
                    EnableCpuMemArena = true,
                    // Set ExecutionMode if desired (e.g., SEQUENTIAL for potential latency benefit)
                    // ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
                    GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
                };

                _session = new InferenceSession(modelPath, sessionOptions);
                _logger.LogInformation("Silero VAD ONNX model loaded successfully from {ModelPath}", modelPath);

                // --- Model Specific Config ---
                if (_options.SampleRate != 16000 && _options.SampleRate != 8000)
                {
                    _logger.LogWarning("VAD configured with unsupported sample rate {SampleRate}. Forcing to 16000.", _options.SampleRate);
                    _options.SampleRate = 16000; // Or throw error?
                }
                // Set window size based on sample rate (matching reference code)
                _windowSizeSamples = _options.SampleRate == 16000 ? 512 : 256;
                _windowSizeBytes = _windowSizeSamples * 2; // 16-bit PCM
                                                           // Use provided window size if explicitly set and valid
                if (_options.WindowSizeSamples > 0 && (_options.WindowSizeSamples == 512 || _options.WindowSizeSamples == 256))
                {
                    _windowSizeSamples = _options.WindowSizeSamples;
                    _windowSizeBytes = _windowSizeSamples * 2;
                }
                else if (_options.WindowSizeSamples > 0)
                {
                    _logger.LogWarning("Ignoring provided WindowSizeSamples ({ProvidedSize}), using default ({CalculatedSize}) based on sample rate.", _options.WindowSizeSamples, _windowSizeSamples);
                }
                // --- End Model Specific Config ---

                _minSilenceSamples = (_options.SampleRate * _options.MinSilenceDurationMs) / 1000;
                _minSpeechSamples = (_options.SampleRate * _options.MinSpeechDurationMs) / 1000;
                _speechPadSamples = (_options.SampleRate * _options.SpeechPadMs) / 1000;

                // Resize buffer based on final window size
                ArrayPool<byte>.Shared.Return(_buffer); // Return initial small buffer
                _buffer = ArrayPool<byte>.Shared.Rent(_windowSizeBytes);

                _logger.LogInformation("VAD Initialized: SampleRate={SampleRate}, WindowSamples={WindowSamples}, MinSilenceMs={MinSilenceMs}, MinSpeechMs={MinSpeechMs}, PaddingMs={SpeechPadMs}, Threshold={Threshold}",
                    _options.SampleRate, _windowSizeSamples, _options.MinSilenceDurationMs, _options.MinSpeechDurationMs, _options.SpeechPadMs, _options.Threshold);

                Reset(); // Initialize state variables and model hidden states
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Silero VAD service.");
                Dispose();
                throw;
            }
        }

        public void ProcessAudio(ReadOnlyMemory<byte> pcm16AudioChunk)
        {
            if (_session == null) return;
            if (pcm16AudioChunk.Length % 2 != 0) pcm16AudioChunk = pcm16AudioChunk.Slice(0, pcm16AudioChunk.Length - 1);
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
            if (_session == null || _state == null || _srTensor == null) return;

            try
            {
                // 1. Convert PCM16 bytes to float32 tensor
                var floatData = MemoryMarshal.Cast<byte, short>(windowData.Span);
                // Ensure input tensor shape matches model (e.g., [1, _windowSizeSamples])
                var inputTensor = new DenseTensor<float>(new[] { BatchSize, _windowSizeSamples });
                for (int i = 0; i < floatData.Length; i++)
                {
                    // Assuming BatchSize is 1
                    inputTensor.SetValue(i, (float)floatData[i] / 32768.0f);
                }

                // 2. Prepare inputs using correct names and tensors
                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor(AudioInputName, inputTensor),
                    NamedOnnxValue.CreateFromTensor(SrInputName, _srTensor), // Tensor<long>
                    NamedOnnxValue.CreateFromTensor(StateInputName, _state)   // Tensor<float> [2, 1, 128]
                };

                // 3. Run Inference
                using var results = _session.Run(inputs);

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
                _speechDurationSamples += _windowSizeSamples;

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
                _silenceDurationSamples += _windowSizeSamples;

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
            _srTensor = new DenseTensor<long>(new long[] { _options.SampleRate }, new[] { 1 }); // Shape [1]

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
            _session?.Dispose();
            _session = null;
            ArrayPool<byte>.Shared.Return(_buffer);
            GC.SuppressFinalize(this);
        }
    }

}