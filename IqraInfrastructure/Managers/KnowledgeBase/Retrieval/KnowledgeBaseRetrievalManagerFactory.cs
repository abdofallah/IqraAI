using IqraCore.Entities.Helpers;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Embedding;
using IqraInfrastructure.Managers.Rerank;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Repositories.KnowledgeBase.Vector;
using IqraInfrastructure.Repositories.RAG;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.KnowledgeBase.Retrieval
{
    public class KnowledgeBaseRetrievalManagerFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly BusinessManager _businessManager;
        private readonly KnowledgeBaseVectorRepository _knowledgeBaseVectorRepository;
        private readonly RAGKeywordStore _ragKeywordStore;
        private readonly BusinessKnowledgeBaseDocumentRepository _documentRepository;
        private readonly EmbeddingProviderManager _embeddingProviderManager;
        private readonly RerankProviderManager _rerankProviderManager;
        private readonly KnowledgeBaseCollectionsLoadManager _knowledgeBaseCollectionsLoadManager;
        private readonly EmbeddingCacheManager _embeddingCacheManager;

        public KnowledgeBaseRetrievalManagerFactory(
            ILoggerFactory loggerFactory,
            BusinessManager businessManager,
            KnowledgeBaseVectorRepository knowledgeBaseVectorRepository,
            RAGKeywordStore ragKeywordStore,
            BusinessKnowledgeBaseDocumentRepository documentRepository,
            EmbeddingProviderManager embeddingProviderManager,
            RerankProviderManager rerankProviderManager,
            KnowledgeBaseCollectionsLoadManager knowledgeBaseCollectionsLoadManager,
            EmbeddingCacheManager embeddingCacheManager
        )
        {
            _loggerFactory = loggerFactory;
            _businessManager = businessManager;
            _knowledgeBaseVectorRepository = knowledgeBaseVectorRepository;
            _ragKeywordStore = ragKeywordStore;
            _documentRepository = documentRepository;
            _embeddingProviderManager = embeddingProviderManager;
            _rerankProviderManager = rerankProviderManager;
            _knowledgeBaseCollectionsLoadManager = knowledgeBaseCollectionsLoadManager;
            _embeddingCacheManager = embeddingCacheManager;
        }

        public async Task<FunctionReturnResult<KnowledgeBaseRetrievalManager?>> CreateManagerAsync(long businessId, string knowledgeBaseId, string vectorCollectionLoadSessionId, TimeSpan vectorCollectionReleaseExpiry)
        {
            var result = new FunctionReturnResult<KnowledgeBaseRetrievalManager?>();

            try
            {
                var manager = new KnowledgeBaseRetrievalManager(
                    _loggerFactory,
                    _businessManager,
                    _knowledgeBaseVectorRepository,
                    _ragKeywordStore,
                    _documentRepository,
                    _embeddingProviderManager,
                    _rerankProviderManager,
                    _knowledgeBaseCollectionsLoadManager,
                    _embeddingCacheManager,
                    businessId,
                    knowledgeBaseId,
                    vectorCollectionLoadSessionId,
                    vectorCollectionReleaseExpiry
                );

                var initResult = await manager.Initalize();
                if (!initResult.Success)
                {
                    return result.SetFailureResult(
                        $"CreateManagerAsync:{initResult.Code}",
                        initResult.Message
                    );
                }

                return result.SetSuccessResult(manager);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "CreateManagerAsync:EXCEPTION",
                    ex.Message
                );
            }
        }
    }
}
