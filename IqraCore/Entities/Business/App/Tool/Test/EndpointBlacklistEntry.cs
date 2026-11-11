using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Business
{
    public class EndpointBlacklistEntry
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        /// <summary>
        /// The pattern to match against (exact URL, wildcard, or CIDR notation).
        /// Examples: "http://192.168.1.100", "*.internal.company.com", "10.0.0.0/8"
        /// </summary>
        public string Pattern { get; set; } = string.Empty;

        /// <summary>
        /// Type of pattern matching to use.
        /// </summary>
        public EndpointBlacklistPatternType PatternType { get; set; } = EndpointBlacklistPatternType.Exact;

        /// <summary>
        /// Human-readable reason for blacklisting.
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public enum EndpointBlacklistPatternType
    {
        Exact = 0,
        Wildcard = 1,
        CIDR = 2
    }
}
