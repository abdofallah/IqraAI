using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Region
{
    public class RegionData
    {
        [BsonId]
        public string Id { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string CountryRegion { get; set; } = string.Empty;

        public DateTime? DisabledAt { get; set; } = null;

        public List<RegionServer> Servers { get; set; } = new List<RegionServer>();
    }
}
