namespace IqraCore.Entities.Business
{
    public class BusinessWorkingHours
    {
        public BusinessWorkingHourDay Sunday { get; set; }
        public BusinessWorkingHourDay Monday { get; set; }
        public BusinessWorkingHourDay Tuesday { get; set; }
        public BusinessWorkingHourDay Wednesday { get; set; }
        public BusinessWorkingHourDay Thursday { get; set; }
        public BusinessWorkingHourDay Friday { get; set; }
        public BusinessWorkingHourDay Saturday { get; set; }
    }
}
