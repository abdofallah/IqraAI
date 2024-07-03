using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Conversation
{
    public class ConversationAgent
    {
        public long SelectedAgentId { get; set; }
        public long OpeningScriptId { get; set; }
        public AgentConversationTypeENUM ConversationType { get; set; }
        public TimeZone Timezone { get; set; }
    }
}
