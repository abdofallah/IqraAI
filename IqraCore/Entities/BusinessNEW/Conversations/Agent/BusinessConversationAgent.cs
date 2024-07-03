using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessConversationAgent
    {
        public long SelectedAgentId { get; set; }
        public long OpeningScriptId { get; set; }
        public AgentConversationTypeENUM ConversationType { get; set; }
        public TimeZone Timezone { get; set; }
    }
}
