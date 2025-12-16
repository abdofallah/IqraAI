using IqraCore.Entities.Business.App.Agent.Script.Node.StartNode;
using IqraCore.Entities.Helper.Agent;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Business
{
    [BsonKnownTypes(
        typeof(BusinessAppScriptStartNode),
        typeof(BusinessAppScriptUserQueryNode),
        typeof(BusinessAppScriptAIResponseNode),
        typeof(BusinessAppScriptSystemToolNode),
        typeof(BusinessAppScriptCustomToolNode),
        typeof(BusinessAppScriptGoToNodeToolNode)
    )]
    public class BusinessAppScriptNode
    {
        public string Id { get; set; } = "";
        public virtual BusinessAppAgentScriptNodeTypeENUM NodeType { get; set; } = BusinessAppAgentScriptNodeTypeENUM.Unknown;
        public BusinessAppAgentScriptNodePosition Position { get; set; } = new BusinessAppAgentScriptNodePosition();
    }

    public class BusinessAppAgentScriptNodePosition
    {
        public double X { get; set; } = 0;
        public double Y { get; set; } = 0;
    }
}
