using IqraCore.Entities.Helper;

namespace IqraCore.Entities.Conversation
{
    public class ConversationConversationMessageResponseExecuteToolUserAdded : ConversationConversationMessageResponseExecuteTool
    {
        public long ToolId { get; set; }
        public HttpStatusEnum ResultStatus { get; set; }
        public Dictionary<string, string> ArguementValues { get; set; }
    }
}
