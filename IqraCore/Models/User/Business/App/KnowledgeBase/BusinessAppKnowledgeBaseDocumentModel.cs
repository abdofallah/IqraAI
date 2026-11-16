using IqraCore.Entities.Business.App.KnowledgeBase.Document;
using IqraCore.Entities.Business.App.KnowledgeBase.Document.Chunk;
using IqraCore.Entities.Business.App.KnowledgeBase.ENUM;

namespace IqraCore.Models.User.Business.App.KnowledgeBase
{
    public class BusinessAppKnowledgeBaseDocumentModel
    {
        public BusinessAppKnowledgeBaseDocumentModel(BusinessAppKnowledgeBaseDocument data)
        {
            Id = data.Id;
            Name = data.Name;
            Enabled = data.Enabled;
            Status = data.Status;
            FailedReason = data.FailedReason;
            Chunks = data.Chunks;
        }

        public long Id { get; set; }
        public string Name { get; set; }
        public bool Enabled { get; set; }
        public KnowledgeBaseDocumentStatus Status { get; set; }
        public string? FailedReason { get; set; }
        public List<BusinessAppKnowledgeBaseDocumentChunk> Chunks { get; set; }
    }
}
