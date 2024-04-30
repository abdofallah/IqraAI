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

        private const int BufferSize = 16384;

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

            _playbackThread = new Thread(PlaybackLoop);
            _playbackThread.Start();
        }

        public void StopPlayback()
        {
            _readingCancellationToken.Cancel();
            _isPlaybackRunning = false;
            if (_playbackThread != null)
            {
                _playbackThread.Join();
                _playbackThread = null;
            }
        }

        public void ClearAudioData()
        {
            _audioDataQueue = new ConcurrentQueue<byte[]>();
            _modemAudio.ClearReadBuffer();
        }

        public bool IsBufferEmpty()
        {
            return !_isWritingData;
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
                    _isWritingData = true;

                    int offset = FindFirstNonZeroOffset(data);

                    while (offset < data.Length)
                    {
                        if (_readingCancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        int chunkSize = Math.Min(BufferSize, data.Length - offset);

                        byte[] buffer = data.AsSpan(offset, chunkSize).ToArray();

                        _modemAudio.WriteData(buffer, buffer.Length, _readingCancellationToken.Token).GetAwaiter().GetResult();

                        offset += chunkSize;

                        Thread.Sleep(GetAudioBufferDuration16k16bit(chunkSize,0.99));
                    }
                }
                else
                {
                    _isWritingData = false;
                }   
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

        private TimeSpan GetAudioBufferDuration16k16bit(int audioBytesLength, double downSampleFactor = 1.0)
        {
            int sampleRate = 16000; // 16,000 Hz
            int bitsPerSample = 16; // 16 bits

            int bytesPerSample = bitsPerSample / 8; // 2 bytes for 16-bit audio
            int numberOfSamples = audioBytesLength / bytesPerSample;

            double durationInSeconds = ((double)numberOfSamples / sampleRate) * downSampleFactor;

            return TimeSpan.FromSeconds(durationInSeconds);
        }
    }
}
