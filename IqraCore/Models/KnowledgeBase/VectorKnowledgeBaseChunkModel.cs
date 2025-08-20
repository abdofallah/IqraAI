namespace IqraCore.Models.KnowledgeBase
{
    public class VectorKnowledgeBaseChunkModel
    {
        public string ChunkId { get; set; }
        public long DocumentId { get; set; }
        public string TextChunk { get; set; }
        public string? ParentChunkId { get; set; }
        public ReadOnlyMemory<float> Embedding { get; set; }
    }
}
