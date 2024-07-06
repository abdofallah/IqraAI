using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppRouteAgent
    {
        public long SelectedAgentId { get; set; } = -1;
        public long OpeningScriptId { get; set; } = -1;
        public AgentConversationTypeENUM ConversationType { get; set; } = AgentConversationTypeENUM.Interruptible;
        public int? InterruptibleConversationTypeWords { get; set; } = null;
        public string Timezone { get; set; } = "0";
        public bool CallerNumberInContext { get; set; } = true;
        public bool RouteNumberInContext { get; set; } = true;
    }
}
