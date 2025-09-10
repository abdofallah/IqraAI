namespace IqraCore.Entities.Business
{
    public class BusinessAppWebCampaignConfiguration
    {
        public BusinessAppWebCampaignConfigurationTimeoutsConfig Timeouts { get; set; } = new BusinessAppWebCampaignConfigurationTimeoutsConfig();
    }

    public class BusinessAppWebCampaignConfigurationTimeoutsConfig
    {
        public int NotifyOnSilenceMS { get; set; } = 10000;
        public int EndOnSilenceMS { get; set; } = 30000;
        public int MaxConversationTimeS { get; set; } = 600;
    }
}
