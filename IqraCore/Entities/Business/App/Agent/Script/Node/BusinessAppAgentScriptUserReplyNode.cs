using IqraCore.Attributes;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentScriptUserReplyNode : BusinessAppAgentScriptNode
    {
        [MultiLanguageProperty]
        public Dictionary<string, string> Message { get; set; } = new Dictionary<string, string>();
    }
}
