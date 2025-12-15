using IqraCore.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.User
{
    public class UserApiKey
    {
        [BsonId]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public string FriendlyName { get; set; } = string.Empty;

        // This will store the AES-256 encrypted key. It should never be sent to the client.
        [ExcludeInAllEndpoints]
        public string EncryptedKey { get; set; } = string.Empty;

        // This is the public, non-secret identifier (e.g., iqra_...aB1c2D)
        public string DisplayName { get; set; } = string.Empty;

        public DateTime CreatedUtc { get; set; }

        public DateTime? LastUsedUtc { get; set; }

        public List<long> RestrictedToBusinessIds { get; set; } = new List<long>();
    }
}