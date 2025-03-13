using IqraCore.Entities.Conversation.Enum;

namespace IqraCore.Entities.Conversation.Events
{
    public class ConversationSessionStateChangedEventArgs : EventArgs
    {
        public ConversationSessionState PreviousState { get; }
        public ConversationSessionState NewState { get; }
        public string Reason { get; }
        public DateTime Timestamp { get; }

        public ConversationSessionStateChangedEventArgs(ConversationSessionState previousState, ConversationSessionState newState, string reason)
        {
            PreviousState = previousState;
            NewState = newState;
            Reason = reason;
            Timestamp = DateTime.UtcNow;
        }
    }

    public class ConversationMessageAddedEventArgs : EventArgs
    {
        public ConversationMessage Message { get; }

        public ConversationMessageAddedEventArgs(ConversationMessage message)
        {
            Message = message;
        }
    }
}
