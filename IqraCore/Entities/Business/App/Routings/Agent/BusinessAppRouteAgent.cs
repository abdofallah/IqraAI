using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppRouteAgent
    {
        public string SelectedAgentId { get; set; } = string.Empty;
        public string OpeningScriptId { get; set; } = string.Empty;
        public AgentConversationTypeENUM ConversationType { get; set; } = AgentConversationTypeENUM.Interruptible;
        public int? InterruptibleConversationTypeWords { get; set; } = 3;
        public List<string> Timezones { get; set; } = new List<string>();
        public bool CallerNumberInContext { get; set; } = true;
        public bool RouteNumberInContext { get; set; } = true;
    }
}
