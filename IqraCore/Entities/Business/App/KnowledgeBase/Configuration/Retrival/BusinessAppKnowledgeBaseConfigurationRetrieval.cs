using IqraCore.Entities.Business.App.KnowledgeBase.ENUM;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Business.App.KnowledgeBase.Configuration.Retrival
{
    // --- Retrieval ---
    [BsonKnownTypes(typeof(BusinessAppKnowledgeBaseConfigurationVectorRetrieval), typeof(BusinessAppKnowledgeBaseConfigurationFullTextRetrieval), typeof(BusinessAppKnowledgeBaseConfigurationHybridRetrieval))]
    public abstract class BusinessAppKnowledgeBaseConfigurationRetrieval
    {
        public abstract KnowledgeBaseRetrievalType Type { get; set; }
    } 

    public class BusinessAppKnowledgeBaseConfigurationVectorRetrieval : BusinessAppKnowledgeBaseConfigurationRetrieval
    {
        public override KnowledgeBaseRetrievalType Type { get; set; } = KnowledgeBaseRetrievalType.VectorSearch;
        public int TopK { get; set; } = 3;
        public bool UseScoreThreshold { get; set; } = false;
        public double? ScoreThreshold { get; set; } = null;
        public RerankSettings Rerank { get; set; } = new();
    }

    public class BusinessAppKnowledgeBaseConfigurationFullTextRetrieval : BusinessAppKnowledgeBaseConfigurationRetrieval
    {
        public override KnowledgeBaseRetrievalType Type { get; set; } = KnowledgeBaseRetrievalType.FullTextSearch;
        public int TopK { get; set; } = 3;
        public RerankSettings Rerank { get; set; } = new();
    }

    public class BusinessAppKnowledgeBaseConfigurationHybridRetrieval : BusinessAppKnowledgeBaseConfigurationRetrieval
    {
        public override KnowledgeBaseRetrievalType Type { get; set; } = KnowledgeBaseRetrievalType.HybirdSearch;
        public KnowledgeBaseHybridRetrievalMode Mode { get; set; } = KnowledgeBaseHybridRetrievalMode.WeightedScore;

        // WeightedScore
        public double? Weight { get; set; } = 0.7;

        // Rerank
        public BusinessAppAgentIntegrationData? RerankIntegration { get; set; } = null;

        // Common
        public int TopK { get; set; } = 3;
        public bool UseScoreThreshold { get; set; } = false;
        public double? ScoreThreshold { get; set; } = null;
      
    }

    // Common
    public class RerankSettings
    {
        public bool Enabled { get; set; } = false;
        public BusinessAppAgentIntegrationData? Integration { get; set; } = null;
    }
}
