namespace IqraCore.Entities.Conversation.Events
{
    public class ConversationAudioGeneratedEventArgs : EventArgs
    {
        public byte[] AudioData { get; }
        public string TargetClientId { get; }
        public DateTime Timestamp { get; }

        public ConversationAudioGeneratedEventArgs(byte[] audioData, string targetClientId = null)
        {
            AudioData = audioData;
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
