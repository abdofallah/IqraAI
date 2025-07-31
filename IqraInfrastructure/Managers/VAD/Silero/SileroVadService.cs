using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.TTS;
using IqraCore.Interfaces.VAD;
using IqraInfrastructure.Helpers.Audio;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Runtime.InteropServices;

namespace IqraInfrastructure.Managers.VAD.Silero
{
    public class SileroVadService : IVadService
    {
        private static int SileroVadSampleRate = 16000;
        private static int Silero16khzWindowSizeSamples = 512;

        private readonly ILogger<SileroVadService> _logger;
        private readonly SileroVadOnnxModel SileroVadOnnxModel;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly List<float> _buffer;

        private VadOptions _options;
        private Task _loopTask;

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


        public event EventHandler<VadEventArgs>? VoiceActivityChanged;

        public SileroVadService(ILogger<SileroVadService> logger)
        {
            _logger = logger;

            SileroVadOnnxModel = new SileroVadOnnxModel();

            _buffer = new List<float>();

            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Initialize(VadOptions options)
        {
            _options = options;

            try
            {
                _minSilenceSamples = SileroVadSampleRate * _options.MinSilenceDurationMs / 1000;
                _minSpeechSamples = SileroVadSampleRate * _options.MinSpeechDurationMs / 1000;
                _speechPadSamples = SileroVadSampleRate * _options.SpeechPadMs / 1000;

                _loopTask = Task.Run(RunLoop, _cancellationTokenSource.Token);
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
            if (SileroVadOnnxModel == null) return;
            if (audioChunkData.Length == 0) return;

            var sourcePcm32FloatProvider = AudioConversationHelper.CreatePcm32FloatProvider(
                audioChunkData,
                new TTSProviderAvailableAudioFormat()
                {
                    Encoding = _options.AudioEncodingType,
                    SampleRateHz = _options.SampleRate,
                    BitsPerSample = _options.BitsPerSample
                }
            );

            var sourcePcm32ResampleTo16khz = AudioConversationHelper.CreateResampler(
                sourcePcm32FloatProvider,
                new AudioRequestDetails()
                {
                    RequestedEncoding = AudioEncodingTypeEnum.PCM,
                    RequestedSampleRateHz = 16000,
                    RequestedBitsPerSample = 32
                }
            );

            float[] floatArray = new float[Silero16khzWindowSizeSamples];

            int currentRead = 0;
            while ((currentRead = sourcePcm32ResampleTo16khz.Read(floatArray, 0, Silero16khzWindowSizeSamples)) > 0)
            {
                _buffer.AddRange(floatArray.Take(currentRead));
            }      
        }

        private async void RunLoop()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                if (_buffer.Count > Silero16khzWindowSizeSamples)
                {
                    float[] currentChunk = _buffer.Take(Silero16khzWindowSizeSamples).ToArray();
                    _buffer.RemoveRange(0, Silero16khzWindowSizeSamples);

                    ProcessWindow(currentChunk);
                }

                await Task.Delay(10);
            }
        }

        private void ProcessWindow(float[] windowData)
        {
            if (SileroVadOnnxModel == null) return;

            try
            {
                _speechProbability = SileroVadOnnxModel.Call(new[] { windowData }, SileroVadSampleRate)[0];

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

            SileroVadOnnxModel.ResetStates();

            _buffer.Clear();
        }

        private void OnVoiceActivityChanged(bool isSpeechDetected)
        {
            try { VoiceActivityChanged?.Invoke(this, new VadEventArgs(isSpeechDetected)); }
            catch (Exception ex) { _logger.LogError(ex, "Error invoking VoiceActivityChanged event handler."); }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            if (_loopTask != null) _loopTask.Wait();
            _buffer.Clear();
            SileroVadOnnxModel.Dispose();

            GC.SuppressFinalize(this);
        }
    }

}