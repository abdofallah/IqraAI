namespace IqraCore.Entities.Conversation.Events
{
    public class ConversationAudioGeneratedEventArgs : EventArgs
    {
        public byte[] AudioData { get; }
        public int SampleRate { get; }
        public int BitsPerSample { get; }

        public DateTime Timestamp { get; }

        public ConversationAudioGeneratedEventArgs(byte[] audioData, int sampleRate, int bitsPerSample)
        {
            AudioData = audioData;
            SampleRate = sampleRate;
            BitsPerSample = bitsPerSample;

            Timestamp = DateTime.UtcNow;
        }
    }

    public class ConversationAudioReceivedEventArgs : EventArgs
    {
        public byte[] AudioData { get; }
        public int SampleRate { get; }
        public int BitsPerSample { get; }

        public DateTime Timestamp { get; }

        public ConversationAudioReceivedEventArgs(byte[] audioData, int sampleRate, int bitsPerSample)
        {
            AudioData = audioData;
            SampleRate = sampleRate;
            BitsPerSample = bitsPerSample;

            Timestamp = DateTime.UtcNow;
        }
    }
}
