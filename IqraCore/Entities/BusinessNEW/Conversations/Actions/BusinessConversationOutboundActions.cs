namespace IqraCore.Entities.BusinessNEW.Conversations.Number
{
    public class BusinessConversationOutboundActions : BusinessConversationActions
    {
        public BusinessAppRouteActionTool DeclinedTool { get; set; }
        public BusinessAppRouteActionTool MisscallTool { get; set; }
        public BusinessAppRouteActionTool PickedupTool { get; set; }
        public BusinessAppRouteActionTool EndedTool { get; set; }
    }
}
