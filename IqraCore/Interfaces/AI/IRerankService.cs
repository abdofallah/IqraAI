namespace IqraCore.Interfaces.AI
{
    // Represents a single document after reranking
    public class RerankedDocument
    {
        public int OriginalIndex { get; set; }
        public string Text { get; set; }
        public double RelevanceScore { get; set; }
    }

    // Represents the overall result from a rerank operation
    public class RerankResult
    {
        public List<RerankedDocument>? RerankedDocuments { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public interface IRerankService : IDisposable
    {
        Task<RerankResult> RerankAsync(string query, List<string> documents, int topN);
    }
}
