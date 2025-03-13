using IqraCore.Entities.Conversation.Enum;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Conversation
{
    public class ConversationState
    {
        [BsonId]
        public string Id { get; set; }
        public long BusinessId { get; set; }
        public string RouteId { get; set; }
        public string QueueId { get; set; }
        public ConversationSessionState Status { get; set; } = ConversationSessionState.Created;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime LastActivityTime { get; set; }
        public string LanguageCode { get; set; }
        public List<ConversationClientInfo> Clients { get; set; } = new List<ConversationClientInfo>();
        public List<ConversationAgentInfo> Agents { get; set; } = new List<ConversationAgentInfo>();
        public List<ConversationMessageData> Messages { get; set; } = new List<ConversationMessageData>();
        public List<ConversationLogEntry> Logs { get; set; } = new List<ConversationLogEntry>();
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
        public ConversationMetrics Metrics { get; set; } = new ConversationMetrics();
    }
}