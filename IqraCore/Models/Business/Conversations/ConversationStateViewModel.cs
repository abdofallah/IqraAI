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

        public List<ConversationStateMessageViewModel> Messages { get; set; }

        public List<ConversationStateLogViewModel> Logs { get; set; }
    }

    public class ConversationStateClientViewModel
    {
        public string ClientId { get; set; }
        public ConversationClientType ClientType { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }
        public string? LeaveReason { get; set; }

        public ConversationMemberAudioCompilationStatus AudioCompilationStatus { get; set; }   
        public string? AudioUrl { get; set; }
        public string? AudioFailedReason { get; set; }
    }

    public class ConversationStateAgentViewModel
    {
        public string AgentId { get; set; }
        public ConversationAgentType AgentType { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }
        public string? LeaveReason { get; set; }

        public ConversationMemberAudioCompilationStatus AudioCompilationStatus { get; set; }
        public string? AudioUrl { get; set; }
        public string? AudioFailedReason { get; set; }
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
