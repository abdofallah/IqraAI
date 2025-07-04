namespace IqraCore.Models.User
{
    public class UsageChartData
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<decimal> Data { get; set; } = new List<decimal>();
    }

    public class GetUsageSummaryModel
    {
        public int TotalCalls { get; set; }
        public decimal TotalDurationMinutes { get; set; }
        public decimal TotalCost { get; set; }
        public decimal AverageDurationSeconds { get; set; }
        public decimal AverageCallCost { get; set; }

        public UsageChartData DurationChart { get; set; } = new UsageChartData();
        public UsageChartData CallsChart { get; set; } = new UsageChartData();

        public string ChartTitle { get; set; } = string.Empty;
    }
}
