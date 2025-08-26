using IqraCore.Entities.Interfaces;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace IqraCore.Entities.Embedding
{
    public class EmbeddingCacheEntry
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; }

        public InterfaceEmbeddingProviderEnum ProviderName { get; set; }

        public string EmbeddingConfigJson { get; set; }

        public int EmbeddingConfigVersion { get; set; }

        public string OriginalText { get; set; }

        public List<float> Vector { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAtUtc { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime LastAccessedAtUtc { get; set; }

        public List<EmbeddingCacheEntryReference> ReferencedBy { get; set; } = new List<EmbeddingCacheEntryReference>();
    }

    public class EmbeddingCacheEntryReference
    {
        public long BusinessId { get; set; }

        public string EmbeddingCacheGroupId { get; set; }

        public string EmbeddingCacheEmbeddingId { get; set; }

        public List<string> ReferencedByAgents { get; set; } = new List<string>();

        public int ReferencedCount { get; set; } = 0;

        public DateTime LastAccessedAtUtc { get; set; }
    }
}
