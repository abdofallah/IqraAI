using IqraCore.Entities.Business.App.KnowledgeBase.ENUM;

namespace IqraCore.Models.KnowledgeBase
{
    public class KnowledgeBaseDocumentUpdateChunksModel
    {
        public List<KnowledgeBaseDocumentUpdateAddedChunkModel> Added { get; set; } = new();
        public List<KnowledgeBaseDocumentUpdateEditedChunkModel> Edited { get; set; } = new();
        public List<string> Deleted { get; set; } = new();
    }

    public class KnowledgeBaseDocumentUpdateAddedChunkModel
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public KnowledgeBaseDocumentType Type { get; set; }
        public string? ParentId { get; set; }
    }

    public class KnowledgeBaseDocumentUpdateEditedChunkModel
    {
        public string Id { get; set; }
        public string Text { get; set; }
    }
}
