namespace IqraCore.Entities.Conversation.Events
{
    public class ConversationDTMFReceivedEventArgs
    {
        public string Digit { get; }
        public string TargetAgentId { get; }
        public DateTime Timestamp { get; }

        public ConversationDTMFReceivedEventArgs(string digit, string targetAgentId = null)
        {
            Digit = digit;
            TargetAgentId = targetAgentId;
            Timestamp = DateTime.UtcNow;
        }
    }
}
