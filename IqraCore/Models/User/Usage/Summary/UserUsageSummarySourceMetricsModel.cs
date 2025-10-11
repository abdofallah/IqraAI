namespace IqraCore.Models.User.Usage.Summary
{
    // By Source
    public class UserUsageSummarySourceMetricsModel
    {
        public int TotalCount { get; set; }

        public decimal TotalCost { get; set; }
        // Cost Breakdown
        public decimal TotalPayAsYouGoCost { get; set; }
        public decimal TotalOverageCost { get; set; }

        public Dictionary<string, UserUsageSummarySourceMetricsByFeaturesModel> ConsumptionByFeature { get; set; } = new Dictionary<string, UserUsageSummarySourceMetricsByFeaturesModel>();
    }

    public class UserUsageSummarySourceMetricsByFeaturesModel
    {
        public int TotalCount { get; set; }

        public decimal TotalCost { get; set; }
        // Cost Breakdown
        public decimal TotalPayAsYouGoCost { get; set; }
        public decimal TotalOverageCost { get; set; }

        public decimal TotalQuantity { get; set; }
        public decimal TotalPayAsYouGoQuantity { get; set; }
        public decimal TotalOverageQuantity { get; set; }
        public decimal TotalIncludedUsage { get; set; }
    }
}
