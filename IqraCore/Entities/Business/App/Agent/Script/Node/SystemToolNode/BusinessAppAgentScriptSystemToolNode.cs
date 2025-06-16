using IqraCore.Entities.Helper.Agent;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Business
{
    [BsonKnownTypes(
        typeof(BusinessAppAgentScriptEndCallToolNode),
        typeof(BusinessAppAgentScriptDTMFInputToolNode),
        typeof(BusinessAppAgentScriptTransferToAgentToolNode),
        typeof(BusinessAppAgentScriptAddScriptToContextToolNode),
        typeof(BusinessAppAgentScriptSendSMSToolNode)
    )]
    public class BusinessAppAgentScriptSystemToolNode : BusinessAppAgentScriptNode
    {
        public override BusinessAppAgentScriptNodeTypeENUM NodeType { get; set; } = BusinessAppAgentScriptNodeTypeENUM.ExecuteSystemTool;
        public virtual BusinessAppAgentScriptNodeSystemToolTypeENUM ToolType { get; set; } = BusinessAppAgentScriptNodeSystemToolTypeENUM.Unknown;
    }
}
