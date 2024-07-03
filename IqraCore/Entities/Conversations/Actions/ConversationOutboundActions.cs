using IqraCore.Entities.BusinessNEW;

namespace IqraCore.Entities.Conversation
{
    public class ConversationOutboundActions : ConversationActions
    {
        public BusinessAppRouteActionTool DeclinedTool { get; set; }
        public BusinessAppRouteActionTool MisscallTool { get; set; }
        public BusinessAppRouteActionTool PickedupTool { get; set; }
        public BusinessAppRouteActionTool EndedTool { get; set; }
    }
}
