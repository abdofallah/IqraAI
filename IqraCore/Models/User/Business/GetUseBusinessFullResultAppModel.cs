using IqraCore.Entities.Business;
using IqraCore.Entities.Business.App.KnowledgeBase;
using IqraCore.Models.User.Business.App.Agent;

namespace IqraCore.Models.User.Business
{
    public class GetUseBusinessFullResultAppModel
    {
        public GetUseBusinessFullResultAppModel(BusinessApp data)
        {
            Id = data.Id;
            Context = data.Context;
            Tools = data.Tools;
            Agents = data.Agents.Select(x => new BusinessAppAgentModel(x)).ToList();
            Scripts = data.Scripts;
            Integrations = data.Integrations;
            Cache = data.Cache;
            Routings = data.Routings;
            Numbers = data.Numbers;
            KnowledgeBases = data.KnowledgeBases;
            TelephonyCampaigns = data.TelephonyCampaigns;
            WebCampaigns = data.WebCampaigns;
            PostAnalysis = data.PostAnalysis;
        }

        public long Id { get; set; }

        public BusinessAppContext Context { get; set; } = new BusinessAppContext();
        public List<BusinessAppTool> Tools { get; set; } = new List<BusinessAppTool>();
        public List<BusinessAppAgentModel> Agents { get; set; } = new List<BusinessAppAgentModel>();
        public List<BusinessAppScript> Scripts { get; set; } = new List<BusinessAppScript>();
        public List<BusinessAppIntegration> Integrations { get; set; } = new List<BusinessAppIntegration>();
        public BusinessAppCache Cache { get; set; } = new BusinessAppCache();
        public List<BusinessAppRoute> Routings { get; set; } = new List<BusinessAppRoute>();
        public List<BusinessNumberData> Numbers { get; set; } = new List<BusinessNumberData>();
        public List<BusinessAppKnowledgeBase> KnowledgeBases { get; set; } = new List<BusinessAppKnowledgeBase>();
        public List<BusinessAppTelephonyCampaign> TelephonyCampaigns { get; set; } = new List<BusinessAppTelephonyCampaign>();
        public List<BusinessAppWebCampaign> WebCampaigns { get; set; } = new List<BusinessAppWebCampaign>();
        public List<BusinessAppPostAnalysis> PostAnalysis { get; set; } = new List<BusinessAppPostAnalysis>();
    }
}
