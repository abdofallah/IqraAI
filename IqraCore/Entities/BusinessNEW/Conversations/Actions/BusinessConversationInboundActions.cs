namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessConversationInboundActions : BusinessConversationActions
    {
        public BusinessConversationNumberActionTool RingingTool { get; set; }
        public BusinessConversationNumberActionTool CallPickedTool { get; set; }
        public BusinessConversationNumberActionTool CallEndedTool { get; set; }
    }
}
