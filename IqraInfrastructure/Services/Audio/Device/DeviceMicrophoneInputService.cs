using IqraCore.Interfaces;
using NAudio.Wave;

namespace IqraInfrastructure.Services.Audio.Device
{
    public class DeviceMicrophoneInputService : IAudioInputService
    {
        private WaveInEvent _waveInEvent;
        private event EventHandler<(byte[], int)> _audioDataReceived;

        public event EventHandler<(byte[], int)> AudioDataReceived
        {
            add { _audioDataReceived += value; }
            remove { _audioDataReceived -= value; }
        }

        public void Initialize()
        {
            _waveInEvent = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 16, 1),
                BufferMilliseconds = 100
            };
            _waveInEvent.DataAvailable += OnDataAvailable;
        }

        public void StartRecording()
        {
            _waveInEvent.StartRecording();
        }

        public void StopRecording()
        {
            _waveInEvent.StopRecording();
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            byte[] buffer = new byte[e.BytesRecorded];
            Array.Copy(e.Buffer, buffer, e.BytesRecorded);
            _audioDataReceived?.Invoke(this, (buffer, e.BytesRecorded));
        }

        public void ClearBuffer()
        {
            // nothing
        }
    }
}