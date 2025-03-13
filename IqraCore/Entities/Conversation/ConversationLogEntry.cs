using IqraCore.Entities.Conversation.Enum;

namespace IqraCore.Entities.Conversation
{
    public class ConversationLogEntry
    {
        public ConversationLogLevel Level { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string DataJson { get; set; }
    }
}
