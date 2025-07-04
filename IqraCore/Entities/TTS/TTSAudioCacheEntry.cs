using IqraCore.Entities.Interfaces;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.TTS
{
    public class TTSAudioCacheEntry
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; }

        public InterfaceTTSProviderEnum ProviderName { get; set; }

        public string TtsConfigJson { get; set; }
        public int TtsConfigVersion { get; set; }
        public string MinioObjectPath { get; set; }

        public TimeSpan Duration { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime LastAccessedAtUtc { get; set; }


        public long BusinessId { get; set; }
        public string AgentId { get; set; }
    }
}
