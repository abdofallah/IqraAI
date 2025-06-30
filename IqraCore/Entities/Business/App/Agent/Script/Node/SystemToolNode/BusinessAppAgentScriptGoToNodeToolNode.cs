using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentScriptGoToNodeToolNode : BusinessAppAgentScriptSystemToolNode
    {
        public override BusinessAppAgentScriptNodeSystemToolTypeENUM ToolType { get; set; } = BusinessAppAgentScriptNodeSystemToolTypeENUM.GoToNode;

        public string GoToNodeId { get; set; }
    }
}
