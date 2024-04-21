using IqraCore.Interfaces;
using System.Collections.Concurrent;

namespace IqraInfrastructure.Services.Audio.SimcomModem
{
    public class ModemOutputService : IAudioOutputService
    {
        private ConcurrentQueue<byte[]> _audioDataQueue;
        private Thread _playbackThread;
        private bool _isPlaybackRunning;

        public void Initialize()
        {
            _audioDataQueue = new ConcurrentQueue<byte[]>();
            _isPlaybackRunning = false;
        }

        public void EnqueueAudioData(byte[] data)
        {
            
        }

        public void StartPlayback()
        {
            
        }

        public void StopPlayback()
        {
            
        }

        public void ClearAudioData()
        {
            
        }

        public bool IsBufferEmpty()
        {
            
        }

        public TimeSpan BufferAudioDataDuration()
        {
            
        }

        private void PlaybackLoop()
        {
            
        }

        private int FindFirstNonZeroOffset(byte[] data)
        {
            for (int i = 0; i < data.Length; i += 1)
            {
                if (data[i] != 0x00)
                {
                    return i;
                }
            }
            return data.Length;
        }
    }
}
