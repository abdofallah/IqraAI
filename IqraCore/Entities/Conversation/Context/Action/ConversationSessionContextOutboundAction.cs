namespace IqraCore.Entities.Conversation.Context.Action
{
    public class ConversationSessionContextOutboundAction
    {
        public ConversationSessionContextAction BusyAction { get; set; } = new ConversationSessionContextAction();
        public ConversationSessionContextAction NoAnswerAction { get; set; } = new ConversationSessionContextAction();
        public ConversationSessionContextAction CallAnsweredAction { get; set; } = new ConversationSessionContextAction();
        public ConversationSessionContextAction CallEndedAction { get; set; } = new ConversationSessionContextAction();
    }
}
