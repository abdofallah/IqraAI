using IqraCore.Entities.Helper.Call.Outbound;

namespace IqraCore.Entities.Business
{
    public class BusinessAppCampaignConfiguration 
    {
        public BusinessAppCampaignConfigurationRetryConfig RetryOnDecline { get; set; } = new BusinessAppCampaignConfigurationRetryConfig();
        public BusinessAppCampaignConfigurationRetryConfig RetryOnMiss { get; set; } = new BusinessAppCampaignConfigurationRetryConfig();
        public BusinessAppCampaignConfigurationTimeoutsConfig Timeouts { get; set; } = new BusinessAppCampaignConfigurationTimeoutsConfig();
    }

    public class BusinessAppCampaignConfigurationRetryConfig
    {
        public bool Enabled { get; set; } = false;

        // if enabled
        public int? Count { get; set; } = null;
        public int? Delay { get; set; } = null;
        public OutboundCallRetryDelayUnitType? Unit { get; set; } = null;
    }

    public class BusinessAppCampaignConfigurationTimeoutsConfig
    {
        public int PickupDelayMS { get; set; } = 0;
        public int NotifyOnSilenceMS { get; set; } = 10000;
        public int EndOnSilenceMS { get; set; } = 30000;
        public int MaxCallTimeS { get; set; } = 600;
    }
}
