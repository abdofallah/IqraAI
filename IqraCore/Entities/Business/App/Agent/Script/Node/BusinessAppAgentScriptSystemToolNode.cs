using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentScriptSystemToolNode : BusinessAppAgentScriptNode
    {
        public BusinessAppAgentScriptNodeSystemToolTypeENUM ToolType { get; set; }

        public List<BusinessAppAgentScriptNodeToolOutcome> ToolOutcomes { get; set; }
        public Dictionary<string, string> ToolConfiguration { get; set; }
    }
}
