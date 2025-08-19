using IqraCore.Entities.Business;
using IqraCore.Entities.Business.App.KnowledgeBase;
using IqraCore.Entities.Helpers;

namespace IqraCore.Models.RAG
{
    public interface IIndexProcessor
    {
        Task<List<ProcessedDocumentChunkModel>> TransformAsync(List<ExtractorDocumentModel> rawDocuments, BusinessAppKnowledgeBase knowledgeBase, long documentId);

        Task<FunctionReturnResult> LoadAsync(List<ProcessedDocumentChunkModel> chunks, BusinessAppKnowledgeBase knowledgeBase, BusinessAppIntegration embeddingIntegration);
    }
}
