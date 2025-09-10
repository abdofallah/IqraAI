namespace IqraCore.Entities.Business
{
    public class BusinessAppTelephonyCampaignActions
    { 
        public BusinessAppCampaignActionConfig CallInitiationFailureTool { get; set; } = new BusinessAppCampaignActionConfig();

        public BusinessAppCampaignActionConfig CallInitiatedTool { get; set; } = new BusinessAppCampaignActionConfig();
        public BusinessAppCampaignActionConfig CallDeclinedTool { get; set; } = new BusinessAppCampaignActionConfig();
        public BusinessAppCampaignActionConfig CallMissedTool { get; set; } = new BusinessAppCampaignActionConfig();
        public BusinessAppCampaignActionConfig CallAnsweredTool { get; set; } = new BusinessAppCampaignActionConfig();
        public BusinessAppCampaignActionConfig CallEndedTool { get; set; } = new BusinessAppCampaignActionConfig();
    }
}
