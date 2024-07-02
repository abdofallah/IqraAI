namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessConversationAgent
    {
        public long SelectedAgentId { get; set; }
        public long OpeningScriptId { get; set; }
        public BusinessConversationAgentConversationENUM ConversationType { get; set; }
        public TimeZone Timezone { get; set; }
    }
}
