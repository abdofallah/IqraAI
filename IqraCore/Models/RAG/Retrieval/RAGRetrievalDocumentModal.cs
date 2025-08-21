namespace IqraCore.Models.RAG.Retrieval
{
    public class RAGRetrievalDocumentModal
    {
        public string PageContent { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
