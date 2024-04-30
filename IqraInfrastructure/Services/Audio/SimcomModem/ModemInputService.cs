using IqraCore.Interfaces;
using SimcomModuleManager.Ports;
using System.Diagnostics;

namespace IqraInfrastructure.Services.Audio.SimcomModem
{
    public class ModemInputService : IAudioInputService
    {
        private ModemAudioPort _modemAudio;
        private event EventHandler<(byte[], int)> _audioDataReceived;

        private Task? _recordingLoopTask;
        private CancellationTokenSource _recordingLoopCancellationToken;


        public event EventHandler<(byte[], int)> AudioDataReceived
        {
            add { _audioDataReceived += value; }
            remove { _audioDataReceived -= value; }
        }

        public void Initialize()
        {
            _recordingLoopCancellationToken = new CancellationTokenSource();
            _recordingLoopTask = null;
        }

        public void SetModemAudioModule(ModemAudioPort modemAudio)
        {
            _modemAudio = modemAudio;
        }

        public void StartRecording()
        {
            _recordingLoopCancellationToken = new CancellationTokenSource();
            _recordingLoopTask = StartRecordingLoop();
        }

        public async void StopRecording()
        {
            _recordingLoopCancellationToken.Cancel();

            if (_recordingLoopTask != null)
            {
                while (true)
                {
                    if (_recordingLoopTask.IsCompleted || _recordingLoopTask.IsCanceled || _recordingLoopTask.IsFaulted)
                    {
                        break;
                    }

                    await Task.Delay(100);
                }
            }
            
        }

        private async Task StartRecordingLoop()
        {
            while (true)
            {
                await Task.Delay(50);

                if (_recordingLoopCancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    var result = await _modemAudio.ReadData(_recordingLoopCancellationToken.Token);

                    var buffer = new Span<byte>(result.Item1, 0, result.Item2).ToArray();
                    var bytesRead = result.Item2;

                    if (bytesRead > 0)
                    {
                        _audioDataReceived.Invoke(this, (buffer, bytesRead));
                    }
                }
                catch (Exception ex)
                {
                    if (ex.GetType() == typeof(TimeoutException))
                    {
                        _recordingLoopCancellationToken.Cancel(); // no need but just in case
                        Task.Run(() => { StopRecording(); });
                        break;
                    }
                    else
                    {
                        Console.WriteLine("StartRecordingLoop: " + ex.Message);
                    }
                }
            }
        }

        private TimeSpan GetAudioBufferDuration16k16bit(int audioBytesLength, double downSampleFactor = 1.0)
        {
            int sampleRate = 16000; // 16,000 Hz
            int bitsPerSample = 16; // 16 bits

            int bytesPerSample = bitsPerSample / 8; // 2 bytes for 16-bit audio
            int numberOfSamples = audioBytesLength / bytesPerSample;

            double durationInSeconds = ((double)numberOfSamples / sampleRate) * downSampleFactor;

            return TimeSpan.FromSeconds(durationInSeconds);
        }

        public async void ClearBuffer()
        {
            var result = await _modemAudio.ReadData(_recordingLoopCancellationToken.Token);
        }
    }
}
