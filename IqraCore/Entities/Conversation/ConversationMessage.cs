using IqraCore.Entities.Conversation.Enum;

namespace IqraCore.Entities.Conversation
{
    public class ConversationMessageData
    {
        public string SenderId { get; set; }
        public ConversationSenderRole Role { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ConversationMessage
    {
        public string SenderId { get; }
        public ConversationSenderRole Role { get; }
        public string Content { get; }
        public DateTime Timestamp { get; }

        public ConversationMessage(string senderId, ConversationSenderRole role, string content)
        {
            SenderId = senderId;
            Role = role;
            Content = content;
            Timestamp = DateTime.UtcNow;
        }
    }
}
