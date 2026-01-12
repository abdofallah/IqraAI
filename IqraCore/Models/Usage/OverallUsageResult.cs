using IqraCore.Entities.User.Usage.Enums;

namespace IqraCore.Models.Usage
{
    public class OverallUsageResult
    {
        public string FeatureKey { get; set; } = string.Empty;
        public UserUsageSourceTypeEnum SourceType { get; set; }
        public UserUsageConsumedTypeEnum ConsumedType { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalCost { get; set; }
        public int Count { get; set; }
    }
}
