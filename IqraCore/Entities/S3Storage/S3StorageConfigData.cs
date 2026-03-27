using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.S3Storage
{
    [BsonIgnoreExtraElements]
    public class S3StorageConfigData
    {
        public string Endpoint { get; set; } = string.Empty;
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public bool UseSSL { get; set; } = false;
        public DateTime? DisabledAt = null;
    }
}
