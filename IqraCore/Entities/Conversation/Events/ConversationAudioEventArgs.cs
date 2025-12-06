namespace IqraCore.Entities.Conversation.Events
{
    public class ConversationAudioGeneratedEventArgs : EventArgs
    {
        public byte[] AudioData { get; }
        public int MasterSampleRate { get; }
        public int MasterBitsPerSample { get; }

        public string TargetClientId { get; }
        public DateTime Timestamp { get; }

        public ConversationAudioGeneratedEventArgs(byte[] audioData, int masterSampleRate, int masterBitsPerSample, string targetClientId = null)
        {
            AudioData = audioData;
            MasterSampleRate = masterSampleRate;
            MasterBitsPerSample = masterBitsPerSample;
            TargetClientId = targetClientId;
            Timestamp = DateTime.UtcNow;
        }
    }

    public class ConversationAudioReceivedEventArgs : EventArgs
    {
        public byte[] AudioData { get; }
        public string TargetAgentId { get; }
        public DateTime Timestamp { get; }

        public ConversationAudioReceivedEventArgs(byte[] audioData, string targetAgentId = null)
        {
            AudioData = audioData;
            TargetAgentId = targetAgentId;
            Timestamp = DateTime.UtcNow;
        }
    }
}
