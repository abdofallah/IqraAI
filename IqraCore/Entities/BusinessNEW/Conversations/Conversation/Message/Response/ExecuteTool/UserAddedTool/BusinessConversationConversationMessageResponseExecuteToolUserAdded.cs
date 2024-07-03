using IqraCore.Entities.Helper;

namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessConversationConversationMessageResponseExecuteToolUserAdded : BusinessConversationConversationMessageResponseExecuteTool
    {
        public long ToolId { get; set; }
        public HttpStatusEnum ResultStatus { get; set; }
        public Dictionary<string, string> ArguementValues { get; set; }
    }
}
