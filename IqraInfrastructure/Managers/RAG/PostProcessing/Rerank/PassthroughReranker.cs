using IqraCore.Entities.Business.App.KnowledgeBase.Configuration.Retrival;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.RAG;
using IqraCore.Models.RAG.Retrieval;

namespace IqraInfrastructure.Managers.RAG.PostProcessing.Rerank
{
    public class PassthroughReranker : IReranker
    {
        public Task<FunctionReturnResult> Initalize(BusinessAppKnowledgeBaseConfigurationRetrieval retrivalConfig, long? businessId = null)
        {
            // Does nothing
            var result = new FunctionReturnResult();
            return Task.FromResult(result.SetSuccessResult());
        }

        public Task<List<RAGRetrievalDocumentModal>> RerankAsync(string query, List<RAGRetrievalDocumentModal> documents)
        {
            // Does nothing, just returns the original list.
            return Task.FromResult(documents);
        }
    }
}
