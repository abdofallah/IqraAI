using System;
using System.Collections.Concurrent;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using IqraCore.Interfaces;
using NAudio.Wave;

namespace IqraInfrastructure.Services.Audio
{
    public class DeviceSpeakerOutputService : IAudioOutputService
    {
        private WaveOutEvent _waveOutEvent;
        private BufferedWaveProvider _bufferedWaveProvider;
        private ConcurrentQueue<byte[]> _audioDataQueue;
        private Thread _playbackThread;
        private bool _isPlaybackRunning;
        private const int BufferSize = 8192;

        public void Initialize()
        {
            _waveOutEvent = new WaveOutEvent();
            _bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(16000, 16, 1));
            _waveOutEvent.Init(_bufferedWaveProvider);
            _audioDataQueue = new ConcurrentQueue<byte[]>();
            _isPlaybackRunning = false;
        }

        public void EnqueueAudioData(byte[] data)
        {
            _audioDataQueue.Enqueue(data);
        }

        public void StartPlayback()
        {
            if (!_isPlaybackRunning)
            {
                _isPlaybackRunning = true;
                _playbackThread = new Thread(PlaybackLoop);
                _playbackThread.Start();
            }

            _waveOutEvent.Play();
        }

        public void StopPlayback()
        {
            _isPlaybackRunning = false;
            if (_playbackThread != null)
            {
                _playbackThread.Join();
                _playbackThread = null;
            }
            _waveOutEvent.Stop();
        }

        public void ClearAudioData()
        {
            _audioDataQueue = new ConcurrentQueue<byte[]>();
            _bufferedWaveProvider.ClearBuffer();
        }

        public bool IsBufferEmpty()
        {
            return (_bufferedWaveProvider.BufferedBytes == 0);
        }

        public TimeSpan BufferAudioDataDuration()
        {
            return _bufferedWaveProvider.BufferDuration;
        }

        private void PlaybackLoop()
        {
            while (_isPlaybackRunning)
            {
                if (_audioDataQueue.TryDequeue(out byte[] data))
                {
                    int offset = FindFirstNonZeroOffset(data);

                    while (offset < data.Length)
                    {
                        int chunkSize = Math.Min(BufferSize, data.Length - offset);
                        while (_bufferedWaveProvider.BufferedBytes + chunkSize > _bufferedWaveProvider.BufferLength)
                        {
                            Thread.Sleep(10);
                        }
                        _bufferedWaveProvider.AddSamples(data, offset, chunkSize);
                        offset += chunkSize;
                    }
                }

                Thread.Sleep(10);
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