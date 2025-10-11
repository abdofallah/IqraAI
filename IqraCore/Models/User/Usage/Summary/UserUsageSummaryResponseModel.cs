using IqraCore.Attributes;
using IqraCore.Entities.User.Usage.Enums;

namespace IqraCore.Models.User.Usage.Summary
{
    public class UserUsageSummaryResponseModel
    {
        public decimal TotalCost { get; set; }
        // Cost Breakdown
        public decimal TotalPayAsYouGoCost { get; set; }
        public decimal TotalOverageCost { get; set; }

        public Dictionary<long, UserUsageSummaryBusinessMetricsModel> ByBusiness { get; set; } = new Dictionary<long, UserUsageSummaryBusinessMetricsModel>();
        public Dictionary<UserUsageSourceTypeEnum, UserUsageSummarySourceMetricsModel> BySource { get; set; } = new Dictionary<UserUsageSourceTypeEnum, UserUsageSummarySourceMetricsModel>();
        [KeepOriginalDictionaryKeyCase]
        public Dictionary<string, UserUsageSummaryFeatureMetricsModel> ByFeature { get; set; } = new Dictionary<string, UserUsageSummaryFeatureMetricsModel>();


        [KeepOriginalDictionaryKeyCase]
        public Dictionary<string, UserUsageSummaryStackedChartDataModel> Charts { get; set; } = new Dictionary<string, UserUsageSummaryStackedChartDataModel>();
    }

    // Charts
    public class UserUsageSummaryStackedChartDataModel
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<UserUsageSummaryStackedBarDatasetModel> Datasets { get; set; } = new List<UserUsageSummaryStackedBarDatasetModel>();
    }
    public class UserUsageSummaryStackedBarDatasetModel
    {
        public long BusinessId { get; set; }
        public List<decimal> Data { get; set; } = new List<decimal>();
    }
}
