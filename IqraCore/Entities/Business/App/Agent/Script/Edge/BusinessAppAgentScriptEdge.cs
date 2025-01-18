namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentScriptEdge
    {
        public string Id { get; set; } = "";
        public string SourceNodeId { get; set; } = "";
        public string? SourceNodeToolOutcomeType { get; set; } = null;
        public string TargetNodeId { get; set; } = "";
        public string Label { get; set; } = "";
    }
}
