namespace IqraCore.Entities.Business
{
    public class BusinessAppRouteConfiguration
    {
        public string SelectedRegionId { get; set; } = string.Empty;
        public int PickUpDelayMS { get; set; } = 0;
        public int NotifyOnSilenceMS { get; set; } = 10000;
        public int EndCallOnSilenceMS { get; set; } = 30000;
        public int MaxCallTimeS { get; set; } = 600;
    }
}
