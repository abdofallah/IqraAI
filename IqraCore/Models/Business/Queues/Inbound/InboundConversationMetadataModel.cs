using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Helper.Call.Queue;

namespace IqraCore.Models.Business.Queues.Inbound
{
    public class InboundConversationMetadataModel
    {
        public string QueueId { get; set; }
        public CallQueueStatusEnum Status { get; set; }
        public List<CallQueueLog> Logs { get; set; }

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
        public ConversationSessionEndType? SessionEndType { get; set; } = null;
    }
}
