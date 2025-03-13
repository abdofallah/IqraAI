namespace IqraCore.Entities.Conversation.Events
{
    public class ConversationTextGeneratedEventArgs : EventArgs
    {
        public string Text { get; }
        public string TargetClientId { get; }
        public DateTime Timestamp { get; }

        public ConversationTextGeneratedEventArgs(string text, string targetClientId = null)
        {
            Text = text;
            TargetClientId = targetClientId;
            Timestamp = DateTime.UtcNow;
        }
    }

    public class ConversationTextReceivedEventArgs : EventArgs
    {
        public string Text { get; }
        public DateTime Timestamp { get; }

        public ConversationTextReceivedEventArgs(string text)
        {
            Text = text;
            Timestamp = DateTime.UtcNow;
        }
    }
}
