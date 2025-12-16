using IqraCore.Entities.Helper.Agent;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Business
{
    [BsonKnownTypes(
        typeof(BusinessAppScriptEndCallToolNode),
        typeof(BusinessAppScriptDTMFInputToolNode),
        typeof(BusinessAppScriptTransferToAgentToolNode),
        typeof(BusinessAppScriptAddScriptToContextToolNode),
        typeof(BusinessAppScriptSendSMSToolNode),
        typeof(BusinessAppScriptRetrieveKnowledgeBaseNode)
    )]
    public class BusinessAppScriptSystemToolNode : BusinessAppScriptNode
    {
        public override BusinessAppAgentScriptNodeTypeENUM NodeType { get; set; } = BusinessAppAgentScriptNodeTypeENUM.ExecuteSystemTool;
        public virtual BusinessAppAgentScriptNodeSystemToolTypeENUM ToolType { get; set; } = BusinessAppAgentScriptNodeSystemToolTypeENUM.Unknown;
    }
}
