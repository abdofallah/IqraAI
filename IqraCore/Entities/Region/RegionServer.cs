using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Region
{
    public class RegionServer
    {
        [BsonId]
        public string IpAddress { get; set; } = string.Empty;

        public DateTime? DisabledAt { get; set; } = null;
    }
}
