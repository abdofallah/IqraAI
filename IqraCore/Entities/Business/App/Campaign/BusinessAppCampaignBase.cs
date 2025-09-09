using IqraCore.Entities.Helper.Campaign;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Business
{
    [BsonKnownTypes(typeof(BusinessAppCampaignTelephony), typeof(BusinessAppCampaignWeb))]
    public abstract class BusinessAppCampaignBase
    {
        public string Id { get; set; }

        public abstract BusinessAppCampaignTypeENUM Type { get; set; }

        public BusinessAppCampaignGeneral General { get; set; } = new BusinessAppCampaignGeneral();
        public BusinessAppCampaignAgent Agent { get; set; } = new BusinessAppCampaignAgent();
        public BusinessAppCampaignConfiguration Configuration { get; set; } = new BusinessAppCampaignConfiguration();
        public BusinessAppCampaignVoicemailDetection VoicemailDetection { get; set; } = new BusinessAppCampaignVoicemailDetection();
        public BusinessAppCampaignActions Actions { get; set; } = new BusinessAppCampaignActions();
    }
}
