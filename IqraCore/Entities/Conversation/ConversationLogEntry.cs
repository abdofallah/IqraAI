using IqraCore.Entities.Conversation.Enum;

namespace IqraCore.Entities.Conversation
{
    public class ConversationLogEntry
    {
        public ConversationLogLevel Level { get; set; } = ConversationLogLevel.Information;
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? DataJson { get; set; } = null;
    }
}
