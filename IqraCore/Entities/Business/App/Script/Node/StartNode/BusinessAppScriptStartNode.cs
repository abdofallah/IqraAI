using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business.App.Agent.Script.Node.StartNode
{
    public class BusinessAppScriptStartNode : BusinessAppScriptNode
    {
        public override BusinessAppAgentScriptNodeTypeENUM NodeType { get; set; } = BusinessAppAgentScriptNodeTypeENUM.Start;
    }
}
