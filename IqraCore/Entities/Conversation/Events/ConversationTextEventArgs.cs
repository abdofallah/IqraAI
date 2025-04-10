namespace IqraCore.Entities.Conversation.Events
{
    public class ConversationTextGeneratedEventArgs : EventArgs
    {
        public string Text { get; }
        public string TargetClientId { get; }
        public DateTime Timestamp { get; }
        public bool OnlySave { get; }

        public ConversationTextGeneratedEventArgs(string text, string targetClientId = null, bool onlySave = false)
        {
            Text = text;
            TargetClientId = targetClientId;
            Timestamp = DateTime.UtcNow;
            OnlySave = onlySave;
        }
    }

    public class ConversationTextReceivedEventArgs : EventArgs
    {
        public string Text { get; }
        public string TargetAgentId { get; }
        public DateTime Timestamp { get; }
        public bool OnlySave { get; }

        public ConversationTextReceivedEventArgs(string text, string targetAgentId = null, bool onlySave = false)
        {
            Text = text;
            TargetAgentId = targetAgentId;
            Timestamp = DateTime.UtcNow;
            OnlySave = onlySave;
        }
    }
}
