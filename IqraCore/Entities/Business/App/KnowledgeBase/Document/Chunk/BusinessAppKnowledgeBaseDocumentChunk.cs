using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Business.App.KnowledgeBase.Document.Chunk
{
    [BsonKnownTypes(typeof(BusinessAppKnowledgeBaseDocumentGeneralChunk), typeof(BusinessAppKnowledgeBaseDocumentChildChunk), typeof(BusinessAppKnowledgeBaseDocumentParentChunk))]
    public class BusinessAppKnowledgeBaseDocumentChunk
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}
