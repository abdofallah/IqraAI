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

        // Business Number References
        public List<string> BusinessNumberReferences { get; set; } = new List<string>();

        // Knowledge Base References
        public List<string> KnowledgeBaseEmbeddingModelReferences { get; set; } = new List<string>();
        public List<string> KnowledgeBaseRerankReferences { get; set; } = new List<string>();

        // Post Analysis References
        public List<string> PostAnalysisLLMReferences { get; set; } = new List<string>();

        // Agent References
        public List<string> AgentInterruptionTurnEndViaAILLMReferences { get; set; } = new List<string>();
        public List<string> AgentInterruptionVerificationLLMReferences { get; set; } = new List<string>();

        [MultiLanguageProperty]
        public Dictionary<string, List<string>> AgentSTTReferences { get; set; } = new Dictionary<string, List<string>>();

        [MultiLanguageProperty]
        public Dictionary<string, List<string>> AgentLLMReferences { get; set; } = new Dictionary<string, List<string>>();

        [MultiLanguageProperty]
        public Dictionary<string, List<string>> AgentTTSReferences { get; set; } = new Dictionary<string, List<string>>();

        public List<string> AgentKnowledgeBaseQueryAIRefinementLLMReferences { get; set; } = new List<string>();
        public List<string> AgentKnowledgeBaseSearchStrategyLLMReferences { get; set; } = new List<string>();
    }
}
