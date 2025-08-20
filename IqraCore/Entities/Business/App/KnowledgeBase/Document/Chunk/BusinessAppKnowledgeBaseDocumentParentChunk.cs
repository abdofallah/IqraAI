using IqraCore.Entities.Business.App.KnowledgeBase.ENUM;

namespace IqraCore.Entities.Business.App.KnowledgeBase.Document.Chunk
{
    public class BusinessAppKnowledgeBaseDocumentParentChunk : BusinessAppKnowledgeBaseDocumentChunk
    {
        public override KnowledgeBaseDocumentType Type => KnowledgeBaseDocumentType.Parent;
        public List<string> ChildrenIds { get; set; } = new List<string>();
    }
}
