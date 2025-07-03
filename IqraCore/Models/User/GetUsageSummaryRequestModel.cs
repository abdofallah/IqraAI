namespace IqraCore.Models.User
{
    public class GetUsageSummaryRequestModel
    {
        public UsageTimeRange TimeRange { get; set; }
    }

    public enum UsageTimeRange
    {
        CurrentMonth = 0,
        Last7Days = 1,
        Today = 2
    }
}
