namespace IqraCore.Entities.Conversation.Context
{
    public class ConversationSessionContext
    {
        public ConversationSessionContextLanguage Language { get; set; } = new ConversationSessionContextLanguage();
        public ConversationSessionContextTimeout Timeout { get; set; } = new ConversationSessionContextTimeout();
        public ConversationSessionContextAgent Agent { get; set; } = new ConversationSessionContextAgent();
    }
}
