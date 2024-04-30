namespace IqraCore.Entities.Business
{
    public class BusinessWorkingHourDay
    {
        public bool IsWeekend { get; set; }
        public List<TimeSpan> WorkingHours { get; set; }
    }
}
