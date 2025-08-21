using IqraCore.Entities.Business.App.KnowledgeBase.Configuration.Retrival;
using IqraCore.Entities.Business.App.KnowledgeBase.ENUM;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.RAG;
using IqraCore.Models.RAG.Retrieval;

namespace IqraInfrastructure.Managers.RAG.PostProcessing.Rerank
{
    public class WeightedScoreReranker : IReranker
    {
        private BusinessAppKnowledgeBaseConfigurationHybridRetrieval _retrievalConfig;

        public async Task<FunctionReturnResult> Initalize(BusinessAppKnowledgeBaseConfigurationRetrieval retrivalConfig, long? businessId = null)
        {
            var result = new FunctionReturnResult();

            try
            {
                if (_retrievalConfig is not BusinessAppKnowledgeBaseConfigurationHybridRetrieval hybridConfig)
                {
                    return result.SetFailureResult(
                        "Initalize:UNEXPECTED_CONFIG_TYPE",
                        "Retrieval config is not of type HybridRetrieval"
                    );
                }

                if (hybridConfig.Mode != KnowledgeBaseHybridRetrievalMode.WeightedScore)
                {
                    return result.SetFailureResult(
                        "Initalize:HYBIRD_MODE_NOT_WEIGHTED_SCORE",
                        "Hybird Retrieval mode is not WeightedScore"
                    );
                }

                _retrievalConfig = hybridConfig;

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

        public Task<List<RAGRetrievalDocumentModal>> RerankAsync(string query, List<RAGRetrievalDocumentModal> documents)
        {
            var weight = _retrievalConfig.Weight ?? 0.5;

            foreach (var doc in documents)
            {
                var vectorScore = doc.Metadata.TryGetValue("Score", out var scoreObj) && scoreObj is double vScore ? vScore : 0.0;
                var keywordScore = doc.Metadata.ContainsKey("KeywordMatch") ? 1.0 : 0.0;

                // Calculate the new hybrid score
                var hybridScore = weight * vectorScore + (1 - weight) * keywordScore;
                doc.Metadata["Score"] = hybridScore;
            }

            var rerankedDocs = documents.OrderByDescending(d => d.Metadata["Score"]).ToList();

            return Task.FromResult(rerankedDocs);
        }

        public async ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
        }
    }
}
