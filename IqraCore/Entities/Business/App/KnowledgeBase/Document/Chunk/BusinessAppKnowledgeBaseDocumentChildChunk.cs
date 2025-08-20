using IqraCore.Entities.Business.App.KnowledgeBase.ENUM;

namespace IqraCore.Entities.Business.App.KnowledgeBase.Document.Chunk
{
    public class BusinessAppKnowledgeBaseDocumentChildChunk : BusinessAppKnowledgeBaseDocumentChunk
    {
        public override KnowledgeBaseDocumentType Type => KnowledgeBaseDocumentType.Child;
        public string ParentId { get; set; } = string.Empty;
    }
}
