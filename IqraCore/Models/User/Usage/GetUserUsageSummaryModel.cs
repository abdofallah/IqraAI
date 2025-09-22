namespace IqraCore.Models.User.Usage
{
    public class StackedBarDataset
    {
        public long BusinessId { get; set; }
        public List<decimal> Data { get; set; } = new List<decimal>();
    }

    public class StackedChartData
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<StackedBarDataset> Datasets { get; set; } = new List<StackedBarDataset>();
    }

    public class GetUserUsageSummaryModel
    {
        public int TotalCalls { get; set; }
        public decimal TotalDurationMinutes { get; set; }
        public decimal TotalCost { get; set; }
        public decimal AverageDurationSeconds { get; set; }
        public decimal AverageCallCost { get; set; }

        public StackedChartData DurationChart { get; set; } = new StackedChartData();
        public StackedChartData CallsChart { get; set; } = new StackedChartData();
        public StackedChartData CostChart { get; set; } = new StackedChartData();

        public string ChartTitle { get; set; } = string.Empty;
    }
}
