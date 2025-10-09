using IqraCore.Entities.Business.App.KnowledgeBase.ENUM;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Business.App.KnowledgeBase.Document.Chunk
{
    [BsonKnownTypes(typeof(BusinessAppKnowledgeBaseDocumentGeneralChunk), typeof(BusinessAppKnowledgeBaseDocumentParentChunk), typeof(BusinessAppKnowledgeBaseDocumentChildChunk))]
    public abstract class BusinessAppKnowledgeBaseDocumentChunk
    {
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public abstract KnowledgeBaseDocumentType Type { get; }

        public string Text { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;
    }
}
