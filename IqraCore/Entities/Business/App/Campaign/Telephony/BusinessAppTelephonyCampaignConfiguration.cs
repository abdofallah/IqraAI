using IqraCore.Entities.Helper.Call.Outbound;

namespace IqraCore.Entities.Business
{
    public class BusinessAppTelephonyCampaignConfiguration
    {
        public BusinessAppTelephonyCampaignConfigurationRetryConfig RetryOnDecline { get; set; } = new BusinessAppTelephonyCampaignConfigurationRetryConfig();
        public BusinessAppTelephonyCampaignConfigurationRetryConfig RetryOnMiss { get; set; } = new BusinessAppTelephonyCampaignConfigurationRetryConfig();
        public BusinessAppTelephonyCampaignConfigurationTimeoutsConfig Timeouts { get; set; } = new BusinessAppTelephonyCampaignConfigurationTimeoutsConfig();
    }

    public class BusinessAppTelephonyCampaignConfigurationRetryConfig
    {
        public bool Enabled { get; set; } = false;
        public int? Count { get; set; } = null;
        public int? Delay { get; set; } = null;
        public OutboundCallRetryDelayUnitType? Unit { get; set; } = null;
    }

    public class BusinessAppTelephonyCampaignConfigurationTimeoutsConfig
    {
        public int PickupDelayMS { get; set; } = 0;
        public int NotifyOnSilenceMS { get; set; } = 10000;
        public int EndOnSilenceMS { get; set; } = 30000;
        public int MaxCallTimeS { get; set; } = 600;
    }
}
