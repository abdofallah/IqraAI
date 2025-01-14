namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentScript
    {
        public string Id { get; set; } = "";
        public bool IsDefault { get; set; } = false;
        public bool IsInContext { get; set; } = false;

        public BusinessAppAgentScriptGeneral General { get; set; } = new BusinessAppAgentScriptGeneral();
        public List<BusinessAppAgentScriptNode> Nodes { get; set; } = new List<BusinessAppAgentScriptNode>();
        public List<BusinessAppAgentScriptEdge> Edges { get; set; } = new List<BusinessAppAgentScriptEdge>();
    }  
}
