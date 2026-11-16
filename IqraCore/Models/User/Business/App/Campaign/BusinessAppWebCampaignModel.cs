using IqraCore.Entities.Business;
using IqraCore.Entities.Business.App.Campaign;

namespace IqraCore.Models.User.Business.App.Campaign
{
    public class BusinessAppWebCampaignModel
    {
        public BusinessAppWebCampaignModel(BusinessAppWebCampaign data)
        {
            Id = data.Id;
            General = data.General;
            Agent = data.Agent;
            Configuration = data.Configuration;
            Actions = data.Actions;
            Variables = data.Variables;
            PostAnalysis = data.PostAnalysis;
        }

        public string Id { get; set; }
        public BusinessAppWebCampaignGeneral General { get; set; }
        public BusinessAppWebCampaignAgent Agent { get; set; }
        public BusinessAppWebCampaignConfiguration Configuration { get; set; }
        public BusinessAppWebCampaignActions Actions { get; set; }
        public BusinessAppCampaignVariables Variables { get; set; }
        public BusinessAppCampaignPostAnalysis PostAnalysis { get; set; }
    }
}
