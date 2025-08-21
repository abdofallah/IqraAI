using IqraCore.Entities.Business.App.KnowledgeBase.Configuration.Retrival;
using IqraCore.Entities.Helpers;
using IqraCore.Models.RAG.Retrieval;

namespace IqraCore.Interfaces.RAG
{
    public interface IReranker : IAsyncDisposable
    {
        Task<FunctionReturnResult> Initalize(BusinessAppKnowledgeBaseConfigurationRetrieval retrivalConfig, long? businessId = null);
        Task<List<RAGRetrievalDocumentModal>> RerankAsync(string query, List<RAGRetrievalDocumentModal> documents);
    }
}
