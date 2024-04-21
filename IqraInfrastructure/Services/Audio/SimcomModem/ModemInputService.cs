using IqraCore.Interfaces;
using SimcomModuleManager;

namespace IqraInfrastructure.Services.Audio.SimcomModem
{
    public class ModemInputService : IAudioInputService
    {
        private ModemAudio _modemAudio;
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

        public void SetModemAudioModule(ModemAudio modemAudio)
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
                if (_recordingLoopCancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    var result = await _modemAudio.ReadData(_recordingLoopCancellationToken.Token);

                    var buffer = result.Item1;
                    var bytesRead = result.Item2;

                    if (bytesRead > 0)
                    {
                        _audioDataReceived.Invoke(this, (buffer, bytesRead));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("StartRecordingLoop: " + ex.Message);
                }
            }
        }
    }
}
