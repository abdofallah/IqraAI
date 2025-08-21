namespace IqraCore.Models.RAG.Retrieval
{
    public class RAGRetrievalSourceModel
    {
        public long DocumentId { get; set; }
        public string DocumentName { get; set; } = string.Empty;
        public string ChunkId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public float Score { get; set; }
    }
}
