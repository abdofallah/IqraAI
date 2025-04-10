using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Helper.Telephony;

namespace IqraCore.Models.Business.Conversations
{
    public class InboundConversationMetadataModel
    {
        public string QueueId { get; set; }
        public CallQueueStatusEnum Status { get; set; }

        // Queue Related
        public DateTime EnqueuedAt { get; set; }
        public DateTime? ProcessingStartedAt { get; set; } = null;
        public DateTime? CompletedAt { get; set; } = null;

        // General
        public string NumberId { get; set; }
        public string RouteId { get; set; }
        public string CallerNumber { get; set; }

        // Queue Session Related
        public string? SessionId { get; set; } = null;
        public ConversationSessionState? SessionStatus { get; set; } = null;
    }
}
