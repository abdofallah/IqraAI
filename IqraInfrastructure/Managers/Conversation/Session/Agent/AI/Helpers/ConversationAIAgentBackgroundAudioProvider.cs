using System.Runtime.InteropServices;

namespace IqraInfrastructure.Managers.Conversation.Session.Agent.AI.Helpers
{
    public class AudioFormatDetails
    {
        public int SampleRate { get; set; }
        public int BitsPerSample { get; set; }
    }

    public class ConversationAIAgentBackgroundAudioProvider
    {
        private readonly ReadOnlyMemory<byte> _audioData;
        private int _currentPosition;
        private readonly object _lock = new object();

        public AudioFormatDetails Format { get; }

        public ConversationAIAgentBackgroundAudioProvider(int audioVolume, ReadOnlyMemory<byte> inputData, int sampleRate, int bitsPerSample)
        {
            Format = new AudioFormatDetails
            {
                SampleRate = sampleRate,
                BitsPerSample = bitsPerSample
            };
            _currentPosition = 0;

            if (inputData.IsEmpty)
            {
                _audioData = ReadOnlyMemory<byte>.Empty;
                return;
            }

            float volume = Math.Clamp(audioVolume / 100.0f, 0.0f, 1.0f);
            if (Math.Abs(volume - 1.0f) < 0.001f)
            {
                _audioData = inputData;
            }
            else
            {
                byte[] processedData = inputData.ToArray();
                ApplyVolumeInPlace(processedData, volume, bitsPerSample);
                _audioData = processedData;
            }
        }

        public byte[] GetNextFrame(int requestedBytes)
        {
            if (_audioData.IsEmpty)
            {
                // Return silence if no audio loaded
                return new byte[requestedBytes];
            }

            byte[] resultFrame = new byte[requestedBytes];
            int bytesCopied = 0;

            lock (_lock)
            {
                while (bytesCopied < requestedBytes)
                {
                    int remainingInSource = _audioData.Length - _currentPosition;
                    int toCopy = Math.Min(remainingInSource, requestedBytes - bytesCopied);

                    // Copy the slice
                    _audioData.Slice(_currentPosition, toCopy).CopyTo(resultFrame.AsMemory(bytesCopied));

                    _currentPosition += toCopy;
                    bytesCopied += toCopy;

                    // Loop back to start if we reached the end
                    if (_currentPosition >= _audioData.Length)
                    {
                        _currentPosition = 0;
                    }
                }
            }

            return resultFrame;
        }

        private void ApplyVolumeInPlace(Span<byte> buffer, float volume, int bitsPerSample)
        {
            // 16-bit PCM (Shorts)
            if (bitsPerSample == 16)
            {
                var samples = MemoryMarshal.Cast<byte, short>(buffer);
                for (int i = 0; i < samples.Length; i++)
                {
                    int val = (int)(samples[i] * volume);

                    // Clamp to prevent overflow noise
                    if (val > short.MaxValue) val = short.MaxValue;
                    else if (val < short.MinValue) val = short.MinValue;

                    samples[i] = (short)val;
                }
            }
            // 32-bit Float PCM
            else if (bitsPerSample == 32)
            {
                var samples = MemoryMarshal.Cast<byte, float>(buffer);
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] *= volume;
                }
            }
            // 8-bit PCM (Unsigned)
            else if (bitsPerSample == 8)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    float sample = (float)buffer[i] - 128f; // Center at 0
                    sample *= volume;
                    sample += 128f; // Re-center at 128

                    if (sample > 255) sample = 255;
                    if (sample < 0) sample = 0;

                    buffer[i] = (byte)sample;
                }
            }
        }
    }
}
