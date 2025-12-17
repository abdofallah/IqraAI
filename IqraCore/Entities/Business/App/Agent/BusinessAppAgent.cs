using MongoDB.Bson;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgent
    {
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        public BusinessAppAgentGeneral General { get; set; } = new BusinessAppAgentGeneral();
        public BusinessAppAgentContext Context { get; set; } = new BusinessAppAgentContext();
        public BusinessAppAgentPersonality Personality { get; set; } = new BusinessAppAgentPersonality();
        public BusinessAppAgentUtterances Utterances { get; set; } = new BusinessAppAgentUtterances();
        public BusinessAppAgentInterruption Interruptions { get; set; } = new BusinessAppAgentInterruption();
        public BusinessAppAgentKnowledgeBase KnowledgeBase { get; set; } = new BusinessAppAgentKnowledgeBase();
        public BusinessAppAgentIntegrations Integrations { get; set; } = new BusinessAppAgentIntegrations();
        public BusinessAppAgentCache Cache { get; set; } = new BusinessAppAgentCache();
        public BusinessAppAgentSettings Settings { get; set; } = new BusinessAppAgentSettings();

        // Route/Campaigns References
        public List<string> InboundRoutingReferences { get; set; } = new List<string>();
        public List<string> TelephonyCampaignReferences { get; set; } = new List<string>();
        public List<string> WebCampaignReferences { get; set; } = new List<string>();

        // Add/Transfer Script Node References
        public List<BusinessAppAgentScriptTransferToAgentNodeReference> ScriptTransferToAgentNodeReferences { get; set; } = new List<BusinessAppAgentScriptTransferToAgentNodeReference>();
    }

    public class BusinessAppAgentScriptTransferToAgentNodeReference
    {
        public string ScriptId { get; set; }
        public string NodeId { get; set; }
    }
}
