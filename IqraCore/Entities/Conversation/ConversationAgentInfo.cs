using IqraCore.Entities.Conversation.Enum;

namespace IqraCore.Entities.Conversation
{
    public class ConversationAgentInfo
    {
        public string AgentId { get; set; }
        public ConversationAgentType AgentType { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime? LeftAt { get; set; }
        public string LeaveReason { get; set; }
        public ConversationMemberAudioInfo AudioInfo { get; set; } = new ConversationMemberAudioInfo();
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}
