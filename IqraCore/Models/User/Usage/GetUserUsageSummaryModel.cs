using IqraCore.Entities.Usage;

namespace IqraCore.Models.User.Usage
{
    public class GetUserUsageSummaryModel
    {
        // High-level stats
        public decimal GrandTotalCost { get; set; }
        public OverallUserUsageStatsByTypeResult OverallStats { get; set; }

        // Dynamic charts
        public Dictionary<string, StackedChartData> ChartsByFeature { get; set; } = new Dictionary<string, StackedChartData>();

        public string ChartTitle { get; set; }
    }

    public class StackedChartData
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<StackedBarDataset> Datasets { get; set; } = new List<StackedBarDataset>();
    }

    public class StackedBarDataset
    {
        public long BusinessId { get; set; }
        public List<decimal> Data { get; set; } = new List<decimal>();
    }
}
