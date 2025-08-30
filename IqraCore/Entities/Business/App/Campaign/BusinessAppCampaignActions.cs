namespace IqraCore.Entities.Business
{
    public class BusinessAppCampaignActions
    {
        public BusinessAppCampaignActionConfig Declined { get; set; } = new BusinessAppCampaignActionConfig();
        public BusinessAppCampaignActionConfig Missed { get; set; } = new BusinessAppCampaignActionConfig();
        public BusinessAppCampaignActionConfig Answered { get; set; } = new BusinessAppCampaignActionConfig();
        public BusinessAppCampaignActionConfig Ended { get; set; } = new BusinessAppCampaignActionConfig();
    }

    public class BusinessAppCampaignActionConfig
    {
        public string? ToolId { get; set; }
        public Dictionary<string, object>? Arguments { get; set; }
    }
}
