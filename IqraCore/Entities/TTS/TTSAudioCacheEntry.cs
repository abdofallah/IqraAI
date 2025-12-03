using IqraCore.Entities.Interfaces;
using IqraCore.Entities.S3Storage;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.TTS
{
    public enum TTSAudioCacheStatus
    {
        GENERATING = 1,
        COMPLETE = 2,
        FAILED = 3
    }

    public class TTSAudioCacheEntry
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; } // The CacheKey

        public InterfaceTTSProviderEnum ProviderName { get; set; }
        public string TtsConfigJson { get; set; }
        public int TtsConfigVersion { get; set; }
        public List<TTSAudioCacheEntryReference> ReferencedBy { get; set; } = new List<TTSAudioCacheEntryReference>();

        // --- Fields populated upon successful generation ---
        [BsonIgnoreIfNull]
        public S3StorageFileLink? AudioCacheS3StorageLink { get; set; } = null;

        [BsonIgnoreIfNull]
        public TimeSpan? Duration { get; set; }

        // --- New Fields for State Management & Multi-Region ---
        [BsonRepresentation(BsonType.String)] // Store enum as string for readability
        public TTSAudioCacheStatus Status { get; set; }


        public DateTime CreatedAtUtc { get; set; }
        public DateTime LastUpdatedAtUtc { get; set; }

        [BsonIgnoreIfNull]
        public string ErrorMessage { get; set; }

        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? ExpiresAtUtc { get; set; } // For TTL index on stale 'GENERATING' entries
    }

    public class TTSAudioCacheEntryReference
    {
        public long BusinessId { get; set; }       
        public string AudioCacheGroupId { get; set; }
        public string AudioCacheGroupEntryLanguage { get; set; }
        public string AudioCacheEntryId { get; set; }

        public List<string> ReferencedByAgents { get; set; }
        public int ReferencedCount { get; set; } = 0;
        public DateTime LastAccessedAtUtc { get; set; }
    }
}
