using IqraCore.Attributes;
using IqraCore.Entities.User.Usage.Enums;

namespace IqraCore.Models.User.Usage.Summary
{
    public class UserUsageSummaryBusinessMetricsModel
    {
        public int TotalCount { get; set; }

        public decimal TotalCost { get; set; }
        // Cost Breakdown
        public decimal TotalPayAsYouGoCost { get; set; }
        public decimal TotalOverageCost { get; set; }

        [KeepOriginalDictionaryKeyCase]
        public Dictionary<string, UserUsageSummaryBusinessMetricsByFeatureModel> ConsumptionByFeature { get; set; } = new Dictionary<string, UserUsageSummaryBusinessMetricsByFeatureModel>();
    
        public Dictionary<UserUsageSourceTypeEnum, UserUsageSummaryBusinessMetricsBySourceModel> ConsumptionBySource { get; set; } = new Dictionary<UserUsageSourceTypeEnum, UserUsageSummaryBusinessMetricsBySourceModel>();
    }

    public class UserUsageSummaryBusinessMetricsByFeatureModel
    {
        public int TotalCount { get; set; }

        public decimal TotalQuantity { get; set; }
        // Quantity Breakdown
        public decimal TotalPayAsYouGoQuantity { get; set; }
        public decimal TotalOverageQuantity { get; set; }
        public decimal TotalIncludedUsage { get; set; }

        public decimal TotalCost { get; set; }
        // Cost Breakdown
        public decimal TotalPayAsYouGoCost { get; set; }
        public decimal TotalOverageCost { get; set; }
    }

    public class UserUsageSummaryBusinessMetricsBySourceModel
    {
        public int TotalCount { get; set; }

        public decimal TotalCost { get; set; }
        // Cost Breakdown
        public decimal TotalPayAsYouGoCost { get; set; }
        public decimal TotalOverageCost { get; set; }
    }
}
