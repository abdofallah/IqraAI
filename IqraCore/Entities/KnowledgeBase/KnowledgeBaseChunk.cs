namespace IqraCore.Entities.KnowledgeBase
{
    public class KnowledgeBaseChunk
    {
        public string DocumentName { get; set; }
        public string TextChunk { get; set; }
        public ReadOnlyMemory<float> Embedding { get; set; }
    }
}
