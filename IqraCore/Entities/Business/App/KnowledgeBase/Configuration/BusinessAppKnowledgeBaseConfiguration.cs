using IqraCore.Entities.Business.App.KnowledgeBase.Configuration.Chunking;
using IqraCore.Entities.Business.App.KnowledgeBase.Configuration.Retrival;

namespace IqraCore.Entities.Business.App.KnowledgeBase.Configuration
{
    public class BusinessAppKnowledgeBaseConfiguration
    {
        public BusinessAppKnowledgeBaseConfigurationChunking Chunking { get; set; } = new BusinessAppKnowledgeBaseConfigurationGeneralChunking();
        public BusinessAppAgentIntegrationData Embedding { get; set; } = new BusinessAppAgentIntegrationData();
        public BusinessAppKnowledgeBaseConfigurationRetrieval Retrieval { get; set; } = new BusinessAppKnowledgeBaseConfigurationVectorRetrieval();
    }
}
