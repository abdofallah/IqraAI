namespace IqraCore.Entities
{
    public class Session
    {
        public string SessionId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public SessionAnalytics Analytics { get; set; }
        public List<SessionConversation> Conversation { get; set; }
    }
}