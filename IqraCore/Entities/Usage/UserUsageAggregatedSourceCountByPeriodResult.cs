namespace IqraCore.Entities.Usage
{
    public class UserUsageAggregatedSourceCountByPeriodResult
    {
        public string Period { get; set; } = string.Empty;
        public long BusinessId { get; set; }
        public int Count { get; set; }
    }
}
