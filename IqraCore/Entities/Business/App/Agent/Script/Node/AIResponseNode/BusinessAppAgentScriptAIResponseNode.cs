using IqraCore.Attributes;
using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentScriptAIResponseNode : BusinessAppAgentScriptNode
    {
        public override BusinessAppAgentScriptNodeTypeENUM NodeType { get; set; } = BusinessAppAgentScriptNodeTypeENUM.AIResponse;

        [MultiLanguageProperty]
        public Dictionary<string, string> Response { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, string> Examples { get; set; } = new Dictionary<string, string>();
    }
}
