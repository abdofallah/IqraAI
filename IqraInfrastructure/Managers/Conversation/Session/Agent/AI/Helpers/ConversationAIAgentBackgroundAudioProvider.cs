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

        public ConversationAIAgentBackgroundAudioProvider(ReadOnlyMemory<byte> audioData, int sampleRate, int bitsPerSample)
        {
            if (audioData.IsEmpty)
            {
                _audioData = ReadOnlyMemory<byte>.Empty;
            }
            else
            {
                _audioData = audioData;
            }

            Format = new AudioFormatDetails
            {
                SampleRate = sampleRate,
                BitsPerSample = bitsPerSample
            };
            _currentPosition = 0;
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
    }
}
