using IqraCore.Interfaces.Conversation;

namespace IqraCore.Entities.Conversation.Events
{
    public class ConversationClientDisconnectedEventArgs : EventArgs
    {
        public string Reason { get; }
        public DateTime Timestamp { get; }

        public ConversationClientDisconnectedEventArgs(string reason)
        {
            Reason = reason;
            Timestamp = DateTime.UtcNow;
        }
    }

    public class ConversationClientAddedEventArgs : EventArgs
    {
        public IConversationClient Client { get; }

        public ConversationClientAddedEventArgs(IConversationClient client)
        {
            Client = client;
        }
    }

    public class ConversationClientRemovedEventArgs : EventArgs
    {
        public string ClientId { get; }
        public string Reason { get; }

        public ConversationClientRemovedEventArgs(string clientId, string reason)
        {
            ClientId = clientId;
            Reason = reason;
        }
    }
}
