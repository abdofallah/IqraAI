namespace IqraCore.Models.User.Usage
{
    public enum UsageGroupBy
    {
        Hour = 0,
        Day = 1,
        Month = 2
    }

    public class GetUserUsageSummaryRequestModel
    {
        public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;
        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddMonths(-1).Date;
        public UsageGroupBy GroupBy { get; set; } = UsageGroupBy.Day;
    }
}
