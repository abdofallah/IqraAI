using IqraCore.Entities.Business.App.Campaign;

namespace IqraCore.Entities.Business
{
    public class BusinessAppTelephonyCampaign
    {
        public string Id { get; set; }
        public BusinessAppTelephonyCampaignGeneral General { get; set; } = new BusinessAppTelephonyCampaignGeneral();
        public BusinessAppTelephonyCampaignAgent Agent { get; set; } = new BusinessAppTelephonyCampaignAgent();
        public BusinessAppTelephonyCampaignConfiguration Configuration { get; set; } = new BusinessAppTelephonyCampaignConfiguration();
        public BusinessAppTelephonyCampaignVoicemailDetection VoicemailDetection { get; set; } = new BusinessAppTelephonyCampaignVoicemailDetection();
        public BusinessAppTelephonyCampaignNumberRoute NumberRoute { get; set; } = new BusinessAppTelephonyCampaignNumberRoute();
        public BusinessAppTelephonyCampaignActions Actions { get; set; } = new BusinessAppTelephonyCampaignActions();
        public BusinessAppCampaignVariables Variables { get; set; } = new BusinessAppCampaignVariables();
    }
}
