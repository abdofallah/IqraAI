namespace IqraCore.Models.RAG
{
    public class ProcessedDocumentChunkModel
    {
        public string Id { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public string Hash { get; set; } = string.Empty;

        public float[] Vector { get; set; } = Array.Empty<float>();

        public long OriginalDocumentId { get; set; }

        public Dictionary<string, object> Metadata { get; set; } = new();

        // Properties for parent-child relationship
        public bool IsParent { get; set; } = false;
        public string? ParentId { get; set; }
        public List<ProcessedDocumentChunkModel>? Children { get; set; }
    }
}
