using IqraCore.Entities.BusinessNEW;

namespace IqraCore.Entities.Conversation
{
    public class ConversationWebsocketActions : ConversationActions
    {
        public BusinessAppRouteActionTool StartedTool { get; set; }
        public BusinessAppRouteActionTool EndedTool { get; set; }
    }
}
