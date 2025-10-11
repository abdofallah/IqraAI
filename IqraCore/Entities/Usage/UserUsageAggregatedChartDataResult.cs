using IqraCore.Entities.User.Usage.Enums;

namespace IqraCore.Entities.Usage
{
    public class UserUsageAggregatedChartDataResult
    {
        public string Period { get; set; } = string.Empty;
        public long BusinessId { get; set; }
        public string FeatureKey { get; set; } = string.Empty;
        public UserUsageConsumedTypeEnum ConsumedType { get; set; }
        public decimal Value { get; set; }
    }
}
