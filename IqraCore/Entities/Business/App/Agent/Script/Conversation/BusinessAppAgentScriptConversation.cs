using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentScriptConversation
    {
        public Dictionary<string, string> UserMessage { get; set; }
        public AgentReplyTypeENUM Type { get; set; }
    }
}
