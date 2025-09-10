namespace IqraCore.Entities.Business
{
    public class BusinessAppWebCampaignActions
    {
        public BusinessAppCampaignActionConfig ConversationInitiationFailureTool { get; set; } = new BusinessAppCampaignActionConfig();
        public BusinessAppCampaignActionConfig ConversationInitiatedTool { get; set; } = new BusinessAppCampaignActionConfig();
        public BusinessAppCampaignActionConfig ConversationEndedTool { get; set; } = new BusinessAppCampaignActionConfig();
    }
}
