using IqraCore.Entities.Business;
using IqraCore.Entities.Business.App.KnowledgeBase;
using IqraCore.Models.User.Business.App.Agent;
using IqraCore.Models.User.Business.App.Cache;
using IqraCore.Models.User.Business.App.Campaign;
using IqraCore.Models.User.Business.App.Context;
using IqraCore.Models.User.Business.App.Integration;
using IqraCore.Models.User.Business.App.KnowledgeBase;
using IqraCore.Models.User.Business.App.Number;
using IqraCore.Models.User.Business.App.PostAnalysis;
using IqraCore.Models.User.Business.App.Routing;
using IqraCore.Models.User.Business.App.Script;
using IqraCore.Models.User.Business.App.Tool;

namespace IqraCore.Models.User.Business
{
    public class GetUserBusinessFullResultAppModel
    {
        public GetUserBusinessFullResultAppModel(BusinessApp data)
        {
            Id = data.Id;
            Context = new BusinessAppContextModel(data.Context);
            Tools = data.Tools.Select(x => new BusinessAppToolModel(x)).ToList();
            Agents = data.Agents.Select(x => new BusinessAppAgentModel(x)).ToList();
            Scripts = data.Scripts.Select(x => new BusinessAppScriptModel(x)).ToList();
            Integrations = data.Integrations.Select(x => new BusinessAppIntegrationModel(x)).ToList();
            Cache = new BusinessAppCacheModel(data.Cache);
            Routings = data.Routings.Select(x => new BusinessAppRouteModel(x)).ToList();
            Numbers = data.Numbers.Select(BusinessNumberModel.Create).ToList();
            KnowledgeBases = data.KnowledgeBases.Select(x => new BusinessAppKnowledgeBaseModel(x)).ToList();
            TelephonyCampaigns = data.TelephonyCampaigns.Select(x => new BusinessAppTelephonyCampaignModel(x)).ToList();
            WebCampaigns = data.WebCampaigns.Select(x => new BusinessAppWebCampaignModel(x)).ToList();
            PostAnalysis = data.PostAnalysis.Select(x => new BusinessAppPostAnalysisModel(x)).ToList();
        }

        public long Id { get; set; }

        public BusinessAppContextModel Context { get; set; }
        public List<BusinessAppToolModel> Tools { get; set; } = new List<BusinessAppToolModel>();
        public List<BusinessAppAgentModel> Agents { get; set; } = new List<BusinessAppAgentModel>();
        public List<BusinessAppScriptModel> Scripts { get; set; } = new List<BusinessAppScriptModel>();
        public List<BusinessAppIntegrationModel> Integrations { get; set; } = new List<BusinessAppIntegrationModel>();
        public BusinessAppCacheModel Cache { get; set; }
        public List<BusinessAppRouteModel> Routings { get; set; } = new List<BusinessAppRouteModel>();
        public List<BusinessNumberModel> Numbers { get; set; } = new List<BusinessNumberModel>();
        public List<BusinessAppKnowledgeBaseModel> KnowledgeBases { get; set; } = new List<BusinessAppKnowledgeBaseModel>();
        public List<BusinessAppTelephonyCampaignModel> TelephonyCampaigns { get; set; } = new List<BusinessAppTelephonyCampaignModel>();
        public List<BusinessAppWebCampaignModel> WebCampaigns { get; set; } = new List<BusinessAppWebCampaignModel>();
        public List<BusinessAppPostAnalysisModel> PostAnalysis { get; set; } = new List<BusinessAppPostAnalysisModel>();
    }
}
