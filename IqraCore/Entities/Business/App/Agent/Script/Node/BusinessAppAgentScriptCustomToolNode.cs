namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentScriptCustomToolNode : BusinessAppAgentScriptNode
    {
        public string ToolIdentifier { get; set; } = "";

        public List<BusinessAppAgentScriptNodeToolOutcome> ToolOutcomes { get; set; } = new List<BusinessAppAgentScriptNodeToolOutcome>();
        public Dictionary<string, string> ToolConfiguration { get; set; } = new Dictionary<string, string>();
    }
}
