using IqraCore.Entities.Business.App.KnowledgeBase;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.RAG;
using IqraCore.Models.RAG.Retrieval;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.RAG.PostProcessing.Reorderer;
using IqraInfrastructure.Managers.RAG.PostProcessing.Rerank;
using IqraInfrastructure.Managers.Rerank;

namespace IqraInfrastructure.Managers.RAG.PostProcessing
{
    public record RAGPostProcessingOptions
    {
        public required int TopN { get; init; }
        public double? ScoreThreshold { get; init; }
    }

    public class RAGDataPostProcessor : IAsyncDisposable
    {
        private readonly RerankerFactory _rerankerFactory;

        private IReranker? _reranker = null;

        public RAGDataPostProcessor(BusinessManager businessManager, RerankProviderManager rerankProviderManager)
        {
            _rerankerFactory = new RerankerFactory(businessManager, rerankProviderManager);
        }

        public async Task<FunctionReturnResult> Initalize(long businessId, BusinessAppKnowledgeBase knowledgeBaseData)
        {
            var result = new FunctionReturnResult();

            try
            {
                _reranker = _rerankerFactory.Create(knowledgeBaseData.Configuration.Retrieval);
                var rerankInitalizeResult = await _reranker.Initalize(knowledgeBaseData.Configuration.Retrieval, businessId);
                if (!rerankInitalizeResult.Success)
                {
                    return result.SetFailureResult(
                        $"Initalize:{rerankInitalizeResult.Code}",
                        rerankInitalizeResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "Initalize:EXCEPTION",
                    ex.Message
                );
            }
        }

        public async Task<List<RAGRetrievalDocumentModal>> ProcessAsync(string query, List<RAGRetrievalDocumentModal> documents, RAGPostProcessingOptions options)
        {
            // 1. Rerank
            var rerankedDocuments = await _reranker.RerankAsync(query, documents);

            // 2. Filter by Score Threshold
            var filteredDocuments = rerankedDocuments;
            if (options.ScoreThreshold.HasValue)
            {
                filteredDocuments = rerankedDocuments
                    .Where(d => d.Metadata.TryGetValue("Score", out var scoreObj) && scoreObj is double score && score >= options.ScoreThreshold.Value)
                    .ToList();
            }

            // 3. Reorder // CHECK WHAT THIS DOES
            var reorderedDocuments = RAGLostInTheMiddleReorderer.Reorder(filteredDocuments);

            // 4. Apply TopN limit
            var finalDocuments = reorderedDocuments.Take(options.TopN).ToList();

            return finalDocuments;
        }


        public async ValueTask DisposeAsync()
        {
            if (_reranker != null)
            {
                await _reranker.DisposeAsync();
            }

            GC.SuppressFinalize(this);
        }
    }
}
