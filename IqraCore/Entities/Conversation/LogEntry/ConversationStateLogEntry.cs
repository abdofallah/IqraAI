using IqraCore.Entities.Conversation.Logs.Enums;
using MongoDB.Bson;

namespace IqraCore.Entities.Conversation.Logs
{
    public class ConversationStateLogEntry
    {
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public ConversationStateLogSenderTypeEnum SenderType { get; set; } = ConversationStateLogSenderTypeEnum.Unknown;

        public ConversationStateLogLevelEnum Level { get; set; } = ConversationStateLogLevelEnum.Information;
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // if system
        public string? SystemSenderReference { get; set; } = null;
        public string? ExceptionDataJson { get; set; } = null;
    }
}
