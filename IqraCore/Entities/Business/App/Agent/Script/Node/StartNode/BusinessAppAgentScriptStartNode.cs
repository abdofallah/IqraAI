using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business.App.Agent.Script.Node.StartNode
{
    internal class BusinessAppAgentScriptStartNode : BusinessAppAgentScriptNode
    {
        public override BusinessAppAgentScriptNodeTypeENUM NodeType { get; set; } = BusinessAppAgentScriptNodeTypeENUM.Start;
    }
}
