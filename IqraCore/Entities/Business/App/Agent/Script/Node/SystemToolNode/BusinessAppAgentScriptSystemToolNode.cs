using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentScriptSystemToolNode : BusinessAppAgentScriptNode
    {
        public override BusinessAppAgentScriptNodeTypeENUM NodeType { get; set; } = BusinessAppAgentScriptNodeTypeENUM.ExecuteSystemTool;
        public virtual BusinessAppAgentScriptNodeSystemToolTypeENUM ToolType { get; set; } = BusinessAppAgentScriptNodeSystemToolTypeENUM.Unknown;
    }
}
