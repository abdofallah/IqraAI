namespace IqraCore.Models.User
{
    public class GetUsageSummaryModel
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<decimal> Data { get; set; } = new List<decimal>();
        public string ChartTitle { get; set; } = string.Empty;
    }
}
