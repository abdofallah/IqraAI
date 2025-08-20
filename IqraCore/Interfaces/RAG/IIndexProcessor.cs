using IqraCore.Entities.Business;
using IqraCore.Entities.Business.App.KnowledgeBase;
using IqraCore.Entities.Business.App.KnowledgeBase.Document;
using IqraCore.Entities.Helpers;
using IqraCore.Models.RAG;

namespace IqraCore.Interfaces.RAG
{
    public interface IIndexProcessor
    {
        Task<List<ProcessedDocumentChunkModel>> TransformAsync(List<ExtractorDocumentModel> rawDocuments, BusinessAppKnowledgeBase knowledgeBase, long documentId);

        Task<FunctionReturnResult> LoadAsync(List<ProcessedDocumentChunkModel> chunks, BusinessAppKnowledgeBase knowledgeBase, BusinessAppKnowledgeBaseDocument knowledgeBaseDocument, BusinessAppIntegration embeddingIntegration, long businessId);
    }
}
