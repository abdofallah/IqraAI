namespace IqraCore.Entities.Business
{
    public class BusinessAppRouteNumber
    {
        public long SelectedNumberId { get; set; } = -1;
        public int PickUpDelayMS { get; set; } = 1000;
        public int NotifyOnSilenceMS { get; set; } = 5000;
        public int EndCallOnSilenceMS { get; set; } = 10000;
        public int MaxCallTimeMS { get; set; } = 120;
    }
}
