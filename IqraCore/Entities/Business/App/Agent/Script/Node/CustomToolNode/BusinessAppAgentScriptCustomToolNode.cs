using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentScriptCustomToolNode : BusinessAppAgentScriptNode
    {
        public override BusinessAppAgentScriptNodeTypeENUM NodeType { get; set; } = BusinessAppAgentScriptNodeTypeENUM.ExecuteCustomTool;

        public string ToolIdentifier { get; set; } = "";

        public List<BusinessAppAgentScriptNodeToolOutcome> ToolOutcomes { get; set; } = new List<BusinessAppAgentScriptNodeToolOutcome>();
        public Dictionary<string, string> ToolConfiguration { get; set; } = new Dictionary<string, string>();
    }

    public class BusinessAppAgentScriptNodeToolOutcome
    {
        public string ResponseType { get; set; } = "";
    }
}
