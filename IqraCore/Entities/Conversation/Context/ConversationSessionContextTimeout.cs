namespace IqraCore.Entities.Conversation.Context
{
    public class ConversationSessionContextTimeout
    {
        // Inbound Only
        public int PickUpDelayMS { get; set; } = 0;

        // Common
        public int NotifyOnSilenceMS { get; set; } = 10000;
        public int EndCallOnSilenceMS { get; set; } = 30000;
        public int MaxCallTimeS { get; set; } = 600;
    }
}
