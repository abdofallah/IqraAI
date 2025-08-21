using IqraCore.Entities.Business.App.KnowledgeBase.Configuration.Retrival;
using IqraCore.Entities.Business.App.KnowledgeBase.ENUM;
using IqraCore.Interfaces.RAG;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Rerank;

namespace IqraInfrastructure.Managers.RAG.PostProcessing.Rerank
{
    public class RerankerFactory
    {
        private readonly BusinessManager _businessManager;
        private readonly RerankProviderManager _rerankProviderManager;

        public RerankerFactory(BusinessManager businessManager, RerankProviderManager rerankProviderManager)
        {
            _businessManager = businessManager;
            _rerankProviderManager = rerankProviderManager;
        }

        public IReranker Create(BusinessAppKnowledgeBaseConfigurationRetrieval config)
        {
            if (config is BusinessAppKnowledgeBaseConfigurationHybridRetrieval hybridConfig)
            {
                if (hybridConfig.Mode == KnowledgeBaseHybridRetrievalMode.WeightedScore)
                {
                    return new WeightedScoreReranker();
                }
                else if (hybridConfig.Mode == KnowledgeBaseHybridRetrievalMode.RerankModel && hybridConfig.RerankIntegration != null)
                {
                    return new RerankModelService(_businessManager, _rerankProviderManager);
                }
            }
            else if (config is BusinessAppKnowledgeBaseConfigurationVectorRetrieval vectorConfig && vectorConfig.Rerank.Enabled && vectorConfig.Rerank.Integration != null)
            {
                return new RerankModelService(_businessManager, _rerankProviderManager);
            }
            else if (config is BusinessAppKnowledgeBaseConfigurationFullTextRetrieval fullTextConfig && fullTextConfig.Rerank.Enabled && fullTextConfig.Rerank.Integration != null)
            {
                return new RerankModelService(_businessManager, _rerankProviderManager);
            }

            return new PassthroughReranker();
        }
    }
}
