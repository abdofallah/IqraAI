using IqraCore.Interfaces;
using NAudio.Wave;
using SimcomModuleManager.Ports;
using System.Collections.Concurrent;

namespace IqraInfrastructure.Services.Audio.SimcomModem
{
    public class ModemOutputService : IAudioOutputService
    {
        private ModemAudioPort _modemAudio;

        private ConcurrentQueue<byte[]> _audioDataQueue;
        private Thread _playbackThread;

        private bool _isPlaybackRunning;

        private const int BufferSize = 8192;

        private CancellationTokenSource _readingCancellationToken;

        private bool _isWritingData;

        public void Initialize()
        {
            _audioDataQueue = new ConcurrentQueue<byte[]>();
            _isPlaybackRunning = false;
            _isWritingData = false;
            _readingCancellationToken = new CancellationTokenSource();
        }

        public void SetModemAudioModule(ModemAudioPort modemAudio)
        {
            _modemAudio = modemAudio;
        }

        public void EnqueueAudioData(byte[] data)
        {
            _audioDataQueue.Enqueue(data);
        }

        public void StartPlayback()
        {
            _readingCancellationToken = new CancellationTokenSource();
            _isPlaybackRunning = true;
            PlaybackLoop();
        }

        public void StopPlayback()
        {
            _readingCancellationToken.Cancel();
            _isPlaybackRunning = false;
        }

        public void ClearAudioData()
        {
            _audioDataQueue = new ConcurrentQueue<byte[]>();
            _modemAudio.ClearReadBuffer();
        }

        public bool IsBufferEmpty()
        {
            return _isWritingData;
        }

        private void PlaybackLoop()
        {
            while (_isPlaybackRunning)
            {
                if (_readingCancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (_audioDataQueue.TryDequeue(out byte[] data))
                {
                    int offset = FindFirstNonZeroOffset(data);

                    while (offset < data.Length)
                    {
                        _isWritingData = true;

                        if (_readingCancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        int chunkSize = Math.Min(BufferSize, data.Length - offset);

                        byte[] buffer = data.AsSpan(offset, chunkSize).ToArray();

                        _modemAudio.WriteData(buffer, buffer.Length, _readingCancellationToken.Token).GetAwaiter().GetResult();

                        offset += chunkSize;

                        Thread.Sleep(10);
                    }
                }

                _isWritingData = false;
            }
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
