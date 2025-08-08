namespace IqraCore.Entities.Business.App.KnowledgeBase.Document.Chunk
{
    public class BusinessAppKnowledgeBaseDocumentParentChunk : BusinessAppKnowledgeBaseDocumentChunk
    {
        public List<BusinessAppKnowledgeBaseDocumentChildChunk> Children { get; set; } = new();
    }
}
