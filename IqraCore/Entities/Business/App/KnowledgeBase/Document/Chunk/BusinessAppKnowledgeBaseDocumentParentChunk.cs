namespace IqraCore.Entities.Business.App.KnowledgeBase.Document.Chunk
{
    public class BusinessAppKnowledgeBaseDocumentParentChunk : BusinessAppKnowledgeBaseDocumentChunk
    {
        public List<string> ChildrenIds { get; set; } = new List<string>();
    }
}
