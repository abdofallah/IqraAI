namespace IqraCore.Entities.Business
{
    public class BusinessAppCampaignActions
    { 
        public BusinessAppCampaignActionConfig DeclinedTool { get; set; } = new BusinessAppCampaignActionConfig();
        public BusinessAppCampaignActionConfig MissedTool { get; set; } = new BusinessAppCampaignActionConfig();
        public BusinessAppCampaignActionConfig AnsweredTool { get; set; } = new BusinessAppCampaignActionConfig();
        public BusinessAppCampaignActionConfig EndedTool { get; set; } = new BusinessAppCampaignActionConfig();
    }

    public class BusinessAppCampaignActionConfig
    {
        public string? ToolId { get; set; } = null;
        public Dictionary<string, object>? Arguments { get; set; } = null;
    }
}
