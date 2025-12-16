using IqraCore.Attributes;
using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppScriptRetrieveKnowledgeBaseNode : BusinessAppScriptSystemToolNode
    {
        public override BusinessAppAgentScriptNodeSystemToolTypeENUM ToolType { get; set; } = BusinessAppAgentScriptNodeSystemToolTypeENUM.RetrieveKnowledgeBase;

        [MultiLanguageProperty]
        public Dictionary<string, string> ResponseBeforeExecution { get; set; } = new Dictionary<string, string>();
    }
}
