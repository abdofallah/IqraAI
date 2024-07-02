namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessAppRouteAgent
    {
        public long SelectedAgentId { get; set; }
        public long OpeningScriptId { get; set; }
        public BusinessAppRouteAgentConversationENUM ConversationType { get; set; }
        public int? InterruptibleConversationTypeWords { get; set; }
        public string Timezone { get; set; }
        public bool CallerNumberInContext { get; set; }
        public bool RouteNumberInContext { get; set; }
    }
}
