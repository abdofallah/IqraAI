namespace IqraCore.Entities.Business
{
    public class BusinessAppRouteActions
    {
        public BusinessAppCampaignActionConfig RingingTool { get; set; } = new BusinessAppCampaignActionConfig();
        public BusinessAppCampaignActionConfig CallInitiationFailureTool { get; set; } = new BusinessAppCampaignActionConfig();
        public BusinessAppCampaignActionConfig CallPickedTool { get; set; } = new BusinessAppCampaignActionConfig();
        public BusinessAppCampaignActionConfig CallEndedTool { get; set; } = new BusinessAppCampaignActionConfig();
    }
}
