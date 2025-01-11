namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentScript
    {
        public long Id { get; set; }
        public BusinessAppAgentScriptGeneral General { get; set; }
        public List<BusinessAppAgentScriptNode> Nodes { get; set; }
        public List<BusinessAppAgentScriptEdge> Edges { get; set; }
    }  
}
