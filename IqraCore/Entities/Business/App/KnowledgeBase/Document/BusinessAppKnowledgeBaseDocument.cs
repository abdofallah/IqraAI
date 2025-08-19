using IqraCore.Entities.Business.App.KnowledgeBase.Document.Chunk;
using IqraCore.Entities.Business.App.KnowledgeBase.ENUM;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Business.App.KnowledgeBase.Document
{
    public class BusinessAppKnowledgeBaseDocument
    {
        [BsonId]
        public long Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;

        [BsonRepresentation(BsonType.String)]
        public KnowledgeBaseDocumentStatus Status { get; set; } = KnowledgeBaseDocumentStatus.Processing;
        public string? FailedReason { get; set; } = null;

        public List<BusinessAppKnowledgeBaseDocumentChunk> Chunks { get; set; } = new();
    }
}
