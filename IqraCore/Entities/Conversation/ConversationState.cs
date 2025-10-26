using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.PostAnalysis;
using IqraCore.Entities.Conversation.Turn;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Conversation
{
    public class ConversationState
    {
        [BsonId]
        public string Id { get; set; }

        public string BusinessMasterEmail { get; set; }
        public long BusinessId { get; set; }


        public ConversationSessionInitiationType SessionInitiationType { get; set; }
        public string? QueueId { get; set; }
        public string? WebSessionId { get; set; }

        public string RegionId { get; set; }
        public string ProcessingServerId { get; set; }

        public ConversationSessionState Status { get; set; } = ConversationSessionState.Created;

        public DateTime StartTime { get; set; }
        public DateTime ExpectedEndTimeAt { get; set; }

        public ConversationSessionEndType EndType { get; set; } = ConversationSessionEndType.NotSet;
        public DateTime? EndTime { get; set; }

        public List<ConversationClientInfo> Clients { get; set; } = new List<ConversationClientInfo>();
        public List<ConversationAgentInfo> Agents { get; set; } = new List<ConversationAgentInfo>();

        public List<ConversationTurn> Turns { get; set; } = new List<ConversationTurn>();

        public ConversationMetrics Metrics { get; set; } = new ConversationMetrics();

        public ConversationPostAnalysis PostAnalysis { get; set; } = new ConversationPostAnalysis();
    }
}