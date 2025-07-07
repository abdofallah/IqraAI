using IqraCore.Entities.Helper.Billing;

namespace IqraCore.Models.Usage
{
    public class MinuteUsageRecordModel
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public long BusinessId { get; set; }
        public PlanPricingModel PlanModel { get; set; }
        public decimal MinutesUsed { get; set; }
        public decimal TotalCost { get; set; }
        public string ConversationSessionId { get; set; }
    }
}
