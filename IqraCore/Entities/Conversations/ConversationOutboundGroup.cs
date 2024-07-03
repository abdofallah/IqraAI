namespace IqraCore.Entities.Conversation
{
    public class ConversationOutboundGroup
    {
        public long Id { get; set; }
        public string Identifier { get; set; }
        public string Description { get; set; }
        public List<long> CallSessionIds { get; set; }
    }
}
