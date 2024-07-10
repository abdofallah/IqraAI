using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Region
{
    public class RegionServer
    {
        [BsonId]
        public string Id { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public bool HasSimModules { get; set; } = false;

        public DateTime? DisabledAt { get; set; } = null;
    }
}
