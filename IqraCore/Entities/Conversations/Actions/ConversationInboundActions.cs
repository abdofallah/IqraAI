namespace IqraCore.Entities.Conversation
{
    public class ConversationInboundActions : ConversationActions
    {
        public ConversationNumberActionTool RingingTool { get; set; }
        public ConversationNumberActionTool CallPickedTool { get; set; }
        public ConversationNumberActionTool CallEndedTool { get; set; }
    }
}
