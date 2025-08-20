using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Business.App.KnowledgeBase.Document.Chunk
{
    [BsonKnownTypes(typeof(BusinessAppKnowledgeBaseDocumentGeneralChunk), typeof(BusinessAppKnowledgeBaseDocumentParentChunk), typeof(BusinessAppKnowledgeBaseDocumentChildChunk))]
    public abstract class BusinessAppKnowledgeBaseDocumentChunk
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;
    }
}
