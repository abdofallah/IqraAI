using IqraCore.Attributes;
using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentScriptUserReplyNode : BusinessAppAgentScriptNode
    {
        public override BusinessAppAgentScriptNodeTypeENUM NodeType { get; set; } = BusinessAppAgentScriptNodeTypeENUM.MessageReply;

        [MultiLanguageProperty]
        public Dictionary<string, string> Message { get; set; } = new Dictionary<string, string>();
    }
}
