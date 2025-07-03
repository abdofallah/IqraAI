namespace IqraCore.Models.User
{
    public class MinuteUsageRecordModel
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public long BusinessId { get; set; }
        public string BusinessName { get; set; }
        public decimal MinutesUsed { get; set; }
        public decimal TotalCost { get; set; }
        public string ConversationSessionId { get; set; }
    }
}
