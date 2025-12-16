using IqraCore.Entities.Business;

namespace IqraCore.Models.User.Business.App.Agent
{
    public class BusinessAppAgentModel
    {
        public BusinessAppAgentModel(BusinessAppAgent data)
        {
            Id = data.Id;
            General = data.General;
            Context = data.Context;
            Personality = data.Personality;
            Utterances = data.Utterances;
            Interruptions = data.Interruptions;
            KnowledgeBase = data.KnowledgeBase;
            Integrations = data.Integrations;
            Cache = data.Cache;
            Settings = new BusinessAppAgentSettingsModel(data.Settings);
        }

        public string Id { get; set; }
        public BusinessAppAgentGeneral General { get; set; }
        public BusinessAppAgentContext Context { get; set; }
        public BusinessAppAgentPersonality Personality { get; set; }
        public BusinessAppAgentUtterances Utterances { get; set; }
        public BusinessAppAgentInterruption Interruptions { get; set; }
        public BusinessAppAgentKnowledgeBase KnowledgeBase { get; set; }
        public BusinessAppAgentIntegrations Integrations { get; set; }
        public BusinessAppAgentCache Cache { get; set; }
        public BusinessAppAgentSettingsModel Settings { get; set; }
    }
}
