using IqraCore.Models.RAG;

namespace IqraCore.Interfaces.RAG
{
    public interface IExtractor
    {
        Task<List<ExtractorDocumentModel>> ExtractAsync();
    }
}
