namespace IqraCore.Models.User.Usage.Summary
{
    public class UserUsageSummaryRequestModel
    {
        public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;
        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddMonths(-1).Date;
        public UserUsageGroupBy GroupBy { get; set; } = UserUsageGroupBy.Day;
    }

    public enum UserUsageGroupBy
    {
        Hour = 0,
        Day = 1,
        Month = 2
    }
}
