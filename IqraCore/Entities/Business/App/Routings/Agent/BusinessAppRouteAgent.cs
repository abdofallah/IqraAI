namespace IqraCore.Entities.Business
{
    public class BusinessAppRouteAgent
    {
        public string SelectedAgentId { get; set; } = string.Empty;
        public string OpeningScriptId { get; set; } = string.Empty;
        public BusinessAppRouteAgentInterruption Interruption { get; set; } = new BusinessAppRouteAgentInterruption();
        public List<string> Timezones { get; set; } = new List<string>();
        public bool CallerNumberInContext { get; set; } = true;
        public bool RouteNumberInContext { get; set; } = true;
    }
}
