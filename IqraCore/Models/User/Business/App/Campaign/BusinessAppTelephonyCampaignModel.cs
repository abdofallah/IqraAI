using IqraCore.Entities.Business;
using IqraCore.Entities.Business.App.Campaign;

namespace IqraCore.Models.User.Business.App.Campaign
{
    public class BusinessAppTelephonyCampaignModel
    {
        public BusinessAppTelephonyCampaignModel(BusinessAppTelephonyCampaign data)
        {
            Id = data.Id;
            General = data.General;
            Agent = data.Agent;
            Configuration = data.Configuration;
            VoicemailDetection = data.VoicemailDetection;
            NumberRoute = data.NumberRoute;
            Actions = data.Actions;
            Variables = data.Variables;
            PostAnalysis = data.PostAnalysis;
        }

        public string Id { get; set; }
        public BusinessAppTelephonyCampaignGeneral General { get; set; }
        public BusinessAppTelephonyCampaignAgent Agent { get; set; }
        public BusinessAppTelephonyCampaignConfiguration Configuration { get; set; }
        public BusinessAppTelephonyCampaignVoicemailDetection VoicemailDetection { get; set; }
        public BusinessAppTelephonyCampaignNumberRoute NumberRoute { get; set; }
        public BusinessAppTelephonyCampaignActions Actions { get; set; }
        public BusinessAppCampaignVariables Variables { get; set; }
        public BusinessAppCampaignPostAnalysis PostAnalysis { get; set; }
    }
}
