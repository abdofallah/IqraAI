using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.TTS;
using IqraCore.Interfaces.VAD;
using IqraInfrastructure.Helpers.Audio;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.VAD.Silero
{
    public class SileroVadCore : IDisposable
    {
        // Event that emits the raw speech probability (a float between 0.0 and 1.0) for each processed audio window.
        public event Action<float, TimeSpan>? SpeechProbabilityUpdated;

        // Constants for Silero VAD model requirements.
        private static readonly int SileroVadSampleRate = 16000;
        private static readonly int Silero16khzWindowSizeSamples = 512;

        private readonly ILogger<SileroVadCore> _logger;
        private readonly SileroVadOnnxModel _onnxModel;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly List<float> _buffer = new List<float>();
        private long _bufferReadPosition = 0;

        // Audio format configuration
        private readonly AudioEncodingTypeEnum _audioEncoding;
        private readonly int _sampleRate;
        private readonly int _bitsPerSample;

        // Background Processing Task
        private Task _loopTask;

        public SileroVadCore(ILogger<SileroVadCore> logger, VadOptions options, CancellationToken cancellationToken)
        {
            _logger = logger;
            _onnxModel = new SileroVadOnnxModel();
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _audioEncoding = options.AudioEncodingType;
            _sampleRate = options.SampleRate;
            _bitsPerSample = options.BitsPerSample;
        }

        public void StartAudioProcessingTask()
        {
            _loopTask = Task.Run(RunLoop, _cancellationTokenSource.Token);
        }

        public void ProcessAudio(byte[] audioChunkData)
        {
            if (audioChunkData == null || audioChunkData.Length == 0) return;

            try
            {
                // Helper to create a provider for the source audio format
                var sourcePcm32FloatProvider = AudioConversationHelper.CreatePcm32FloatProvider(
                    audioChunkData,
                    new TTSProviderAvailableAudioFormat()
                    {
                        Encoding = _audioEncoding,
                        SampleRateHz = _sampleRate,
                        BitsPerSample = _bitsPerSample
                    }
                );

                // Helper to resample the audio to the 16kHz required by the Silero VAD model
                var sourcePcm32ResampleTo16khz = AudioConversationHelper.CreateResampler(
                    sourcePcm32FloatProvider,
                    new AudioRequestDetails()
                    {
                        RequestedEncoding = AudioEncodingTypeEnum.PCM,
                        RequestedSampleRateHz = SileroVadSampleRate,
                        RequestedBitsPerSample = 32 // 32-bit float
                    }
                );

                // Read the resampled audio in chunks and add to the buffer
                float[] floatArray = new float[Silero16khzWindowSizeSamples];
                int bytesRead;
                while ((bytesRead = sourcePcm32ResampleTo16khz.Read(floatArray, 0, Silero16khzWindowSizeSamples)) > 0)
                {
                    lock (_buffer) // Lock the buffer for thread-safe modification
                    {
                        _buffer.AddRange(floatArray.Take(bytesRead));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process and convert audio chunk for VAD.");
            }
        }

        private async Task RunLoop()
        {
            _logger.LogDebug("SileroVadCore processing loop started.");
            try
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    float[]? currentChunk = null;
                    lock (_buffer)
                    {
                        if (_buffer.Count >= Silero16khzWindowSizeSamples)
                        {
                            currentChunk = _buffer.Take(Silero16khzWindowSizeSamples).ToArray();
                            _buffer.RemoveRange(0, Silero16khzWindowSizeSamples);
                            _bufferReadPosition += Silero16khzWindowSizeSamples;
                        }
                    }

                    if (currentChunk != null)
                    {
                        ProcessWindow(currentChunk);
                    }
                    else
                    {
                        // If no data, wait briefly before checking again
                        await Task.Delay(10, _cancellationTokenSource.Token);
                    }
                }
            }
            catch (OperationCanceledException) { /* Expected */ }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Silero VAD processing loop exception.");
            }
            _logger.LogDebug("SileroVadCore processing loop stopped.");
        }

        private void ProcessWindow(float[] windowData)
        {
            try
            {
                // Perform inference using the ONNX model
                float speechProbability = _onnxModel.Call(new[] { windowData }, SileroVadSampleRate)[0];

                // Calculate the Timespan for the current buffer read position in TimeSpan, 16khz sample rate, 32bit
                TimeSpan bufferReadPositionTimeSpan = TimeSpan.FromSeconds((double)_bufferReadPosition / SileroVadSampleRate);

                // Emit the raw probability for any subscribers
                SpeechProbabilityUpdated?.Invoke(speechProbability, bufferReadPositionTimeSpan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Silero VAD model inference.");
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            try
            {
                // Wait for the loop to finish, with a timeout
                _loopTask?.Wait(TimeSpan.FromSeconds(1));
            }
            catch (OperationCanceledException) { /* Expected */ }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception while waiting for VAD loop task to complete on dispose.");
            }

            _cancellationTokenSource.Dispose();
            lock (_buffer)
            {
                _buffer.Clear();
            }

            GC.SuppressFinalize(this);
        }
    }
}