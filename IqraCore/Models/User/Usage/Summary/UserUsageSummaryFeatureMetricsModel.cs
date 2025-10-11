namespace IqraCore.Models.User.Usage.Summary
{
    public class UserUsageSummaryFeatureMetricsModel
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
}
