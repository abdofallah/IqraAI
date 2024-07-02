namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessConversationGeneral
    {
        public DateTime Created { get; set; }
        public BusinessConversationENUM ConversationType { get; set; }
        public TimeSpan Timespan { get; set; }
        public bool EndedByUser { get; set; }
    }
}
