using IqraCore.Entities.Helper;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentScriptConversationExecuteToolUserAdded : BusinessAppAgentScriptConversationExecuteTool
    {
        public long ToolId { get; set; }
        public HttpStatusEnum ConditionalStatusToContinue { get; set; }
    }
}
