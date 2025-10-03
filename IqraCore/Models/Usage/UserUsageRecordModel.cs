using IqraCore.Entities.User.Usage.Enums;

namespace IqraCore.Models.Usage
{
    public class UserUsageRecordModel
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public long BusinessId { get; set; }
        public string PlanId { get; set; }
        public string Description { get; set; }
        public UserUsageSourceTypeEnum SourceType { get; set; }
        public string SourceId { get; set; }
        public decimal TotalCost { get; set; }
        public List<ConsumedFeatureModel> ConsumedFeatures { get; set; } = new List<ConsumedFeatureModel>();
    }

    public class ConsumedFeatureModel
    {
        public string FeatureKey { get; set; }
        public string Type { get; set; }
        public decimal Quantity { get; set; }
        public decimal AppliedUnitUsage { get; set; }
        public decimal TotalUsage { get; set; }
    }
}
