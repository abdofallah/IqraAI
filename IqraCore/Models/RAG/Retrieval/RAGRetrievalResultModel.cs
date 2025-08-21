namespace IqraCore.Models.RAG.Retrieval
{
    public class RAGRetrievalResultModel
    {
        public string ContextString { get; set; } = string.Empty;
        public List<RAGRetrievalSourceModel> Sources { get; set; } = new List<RAGRetrievalSourceModel>();
    }
}
