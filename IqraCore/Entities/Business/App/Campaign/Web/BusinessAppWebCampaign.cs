using IqraCore.Entities.Business.App.Campaign;

namespace IqraCore.Entities.Business
{
    public class BusinessAppWebCampaign
    {
        public string Id { get; set; }

        public BusinessAppWebCampaignGeneral General { get; set; } = new BusinessAppWebCampaignGeneral();
        public BusinessAppWebCampaignAgent Agent { get; set; } = new BusinessAppWebCampaignAgent();
        public BusinessAppWebCampaignConfiguration Configuration { get; set; } = new BusinessAppWebCampaignConfiguration();
        public BusinessAppWebCampaignActions Actions { get; set; } = new BusinessAppWebCampaignActions();
        public BusinessAppCampaignVariables Variables { get; set; } = new BusinessAppCampaignVariables();
        public BusinessAppCampaignPostAnalysis PostAnalysis { get; set; } = new BusinessAppCampaignPostAnalysis();
    }
}
