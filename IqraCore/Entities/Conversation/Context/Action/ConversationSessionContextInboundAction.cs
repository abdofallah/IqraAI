namespace IqraCore.Entities.Conversation.Context.Action
{
    public class ConversationSessionContextInboundAction
    {
        public ConversationSessionContextAction CallPickedAction { get; set; } = new ConversationSessionContextAction();
        public ConversationSessionContextAction CallEndedAction { get; set; } = new ConversationSessionContextAction();
    }
}
