using IqraCore.Entities.Business;

namespace IqraCore.Entities.Conversation
{
    public class ConversationWebsocketActions : ConversationActions
    {
        public BusinessAppRouteActionTool StartedTool { get; set; }
        public BusinessAppRouteActionTool EndedTool { get; set; }
    }
}
