using IqraCore.Attributes;
using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentScriptConversation
    {
        [MultiLanguageProperty]
        public Dictionary<string, string> UserMessage { get; set; } = new Dictionary<string, string>();
        public AgentReplyTypeENUM Type { get; set; } = AgentReplyTypeENUM.UserReply;
    } 
}
