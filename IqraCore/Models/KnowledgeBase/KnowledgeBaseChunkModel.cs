namespace IqraCore.Models.KnowledgeBase
{
    public class KnowledgeBaseChunkModel
    {
        public string DocumentName { get; set; }
        public string TextChunk { get; set; }
        public ReadOnlyMemory<float> Embedding { get; set; }
    }
}
