using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppRouteAgent
    {
        public long SelectedAgentId { get; set; }
        public long OpeningScriptId { get; set; }
        public AgentConversationTypeENUM ConversationType { get; set; }
        public int? InterruptibleConversationTypeWords { get; set; }
        public string Timezone { get; set; }
        public bool CallerNumberInContext { get; set; }
        public bool RouteNumberInContext { get; set; }
    }
}
