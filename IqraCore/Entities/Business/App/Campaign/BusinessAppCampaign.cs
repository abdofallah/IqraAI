namespace IqraCore.Entities.Business
{
    public class BusinessAppCampaign
    {
        public string Id { get; set; }

        public BusinessAppCampaignGeneral General { get; set; } = new BusinessAppCampaignGeneral();
        public BusinessAppCampaignAgent Agent { get; set; } = new BusinessAppCampaignAgent();
        public BusinessAppCampaignNumbers Numbers { get; set; } = new BusinessAppCampaignNumbers();
        public BusinessAppCampaignConfiguration Configuration { get; set; } = new BusinessAppCampaignConfiguration();
        public BusinessAppCampaignVoicemailDetection VoicemailDetection { get; set; } = new BusinessAppCampaignVoicemailDetection();
        public BusinessAppCampaignActions Actions { get; set; } = new BusinessAppCampaignActions();
    }
}
