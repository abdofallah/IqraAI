namespace IqraCore.Models.Usage
{
    public class FixedPlanMinuteUsageRecordModel : MinuteUsageRecordModel
    {
        public decimal TotalMinutesDeducted { get; set; }
        public decimal TotalOverageMinutesCharged { get; set; }
        public decimal TotalOverageCost { get; set; }
    }
}
