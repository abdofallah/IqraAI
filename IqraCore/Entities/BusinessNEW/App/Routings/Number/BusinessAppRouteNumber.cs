namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessAppRouteNumber
    {
        public long SelectedNumberId { get; set; }
        public int PickUpDelayMS { get; set; }
        public int NotifyOnSilenceMS { get; set; }
        public int EndCallOnSilenceMS { get; set; }
        public int MaxCallTimeMS { get; set; }
    }
}
