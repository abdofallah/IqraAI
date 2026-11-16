using IqraCore.Entities.Business;

namespace IqraCore.Models.User.Business.App.Routing
{
    public class BusinessAppRouteModel
    {
        public BusinessAppRouteModel(BusinessAppRoute data)
        {
            Id = data.Id;
            General = data.General;
            Language = data.Language;
            Configuration = data.Configuration;
            Numbers = data.Numbers;
            Agent = data.Agent;
            Actions = data.Actions;
            PostAnalysis = data.PostAnalysis;
        }

        public string Id { get; set; }
        public BusinessAppRouteGeneral General { get; set; }
        public BusinessAppRouteLanguage Language { get; set; }
        public BusinessAppRouteConfiguration Configuration { get; set; }
        public List<string> Numbers { get; set; }
        public BusinessAppRouteAgent Agent { get; set; }
        public BusinessAppRouteActions Actions { get; set; }
        public BusinessAppCampaignPostAnalysis PostAnalysis { get; set; }
    }
}
