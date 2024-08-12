using IqraCore.Attributes;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentScriptConversationUserReply : BusinessAppAgentScriptConversation
    {
        [MultiLanguageProperty]
        public string Response { get; set; } = string.Empty;
    }
}
