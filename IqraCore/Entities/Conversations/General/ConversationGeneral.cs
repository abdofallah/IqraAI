namespace IqraCore.Entities.Conversation
{
    public class ConversationGeneral
    {
        public DateTime Created { get; set; }
        public TimeSpan Timespan { get; set; }
        public bool EndedByUser { get; set; }
    }
}
