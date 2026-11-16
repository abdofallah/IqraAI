using IqraCore.Attributes;
using MongoDB.Bson;

namespace IqraCore.Entities.Business
{
    public class BusinessAppIntegration 
    {
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        public string Type { get; set; } = string.Empty;
        public string FriendlyName { get; set; } = string.Empty;
        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();

        [ExcludeInAllEndpoints]
        public Dictionary<string, string> EncryptedFields { get; set; } = new Dictionary<string, string>();

        [ExcludeInAllEndpoints]
        public BusinessAppIntegrationOAuthData? OAuth { get; set; } = null;

        // Business Number References
        [ExcludeInAllEndpoints]
        public List<string> BusinessNumberReferences { get; set; } = new List<string>();

        // Knowledge Base References
        [ExcludeInAllEndpoints]
        public List<string> KnowledgeBaseEmbeddingModelReferences { get; set; } = new List<string>();
        [ExcludeInAllEndpoints]
        public List<string> KnowledgeBaseRerankReferences { get; set; } = new List<string>();

        // Post Analysis References
        [ExcludeInAllEndpoints]
        public List<string> PostAnalysisLLMReferences { get; set; } = new List<string>();

        // Agent References
        [ExcludeInAllEndpoints]
        public List<string> AgentInterruptionTurnEndViaAILLMReferences { get; set; } = new List<string>();
        [ExcludeInAllEndpoints]
        public List<string> AgentInterruptionVerificationLLMReferences { get; set; } = new List<string>();

        [ExcludeInAllEndpoints]
        [MultiLanguageProperty]
        public Dictionary<string, List<string>> AgentSTTReferences { get; set; } = new Dictionary<string, List<string>>();

        [ExcludeInAllEndpoints]
        [MultiLanguageProperty]
        public Dictionary<string, List<string>> AgentLLMReferences { get; set; } = new Dictionary<string, List<string>>();

        [ExcludeInAllEndpoints]
        [MultiLanguageProperty]
        public Dictionary<string, List<string>> AgentTTSReferences { get; set; } = new Dictionary<string, List<string>>();

        [ExcludeInAllEndpoints]
        public List<string> AgentKnowledgeBaseQueryAIRefinementLLMReferences { get; set; } = new List<string>();
        [ExcludeInAllEndpoints]
        public List<string> AgentKnowledgeBaseSearchStrategyLLMReferences { get; set; } = new List<string>();

        // Telephony Campaign
        [ExcludeInAllEndpoints]
        public List<string> TelephonyCampaignVoicemailAdvanceVerificationSTT { get; set; } = new List<string>();
        [ExcludeInAllEndpoints]
        public List<string> TelephonyCampaignVoicemailAdvanceVerificationLLM { get; set; } = new List<string>();
    }
}
