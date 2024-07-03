using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessAppAgentScriptConversation
    {
        public Dictionary<string, string> UserMessage { get; set; }
        public AgentReplyTypeENUM Type { get; set; }
    }
}
