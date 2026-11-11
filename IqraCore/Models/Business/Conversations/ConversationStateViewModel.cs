using IqraCore.Entities.Conversation;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.Logs.Enums;

namespace IqraCore.Models.Business.Conversations
{
    public class ConversationStateViewModel
    {
        public string Id { get; set; }
        public string QueueId { get; set; }
        public ConversationSessionState Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public ConversationSessionEndType EndType { get; set; }

        public List<ConversationStateClientViewModel> Clients { get; set; }
        public List<ConversationStateAgentViewModel> Agents { get; set; }

        // Turn-by-turn conversation data (replaces Messages)
        public List<ConversationTurnViewModel> Turns { get; set; } = new();

        // Post-analysis results (summary, tags, extracted fields)
        public ConversationPostAnalysisViewModel? PostAnalysis { get; set; }

        // Conversation performance metrics
        public ConversationMetrics? Metrics { get; set; }

        // Legacy — kept for backward compatibility, will be removed
        [Obsolete("Use Turns instead. This will be removed in a future update.")]
        public List<ConversationStateMessageViewModel> Messages { get; set; }

        // Legacy — logs are now fetched via separate paginated endpoints
        [Obsolete("Logs are fetched via dedicated log endpoints. This will be removed.")]
        public List<ConversationStateLogViewModel> Logs { get; set; }
    }

    public class ConversationStateClientViewModel
    {
        public string ClientId { get; set; }
        public ConversationClientType ClientType { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }
        public string? LeaveReason { get; set; }

        public string? AudioUrl { get; set; }
    }

    public class ConversationStateAgentViewModel
    {
        public string AgentId { get; set; }
        public ConversationAgentType AgentType { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }
        public string? LeaveReason { get; set; }

        public string? AudioUrl { get; set; }
    }

    public class ConversationStateMessageViewModel
    {
        public string SenderId { get; set; }
        public ConversationSenderRole Role { get; set; }
        public string Content { get; set; }
        public DateTime? Timestamp { get; set; }
    }

    public class ConversationStateLogViewModel
    {
        public ConversationStateLogLevelEnum Level { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
