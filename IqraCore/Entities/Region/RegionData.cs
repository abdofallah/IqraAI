using IqraCore.Attributes;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Region
{
    public class RegionData
    {
        [BsonId]
        public string CountryRegion { get; set; } = "";

        public string CountryCode { get; set; } = string.Empty;     

        public DateTime? DisabledAt { get; set; } = null;

        [ExcludeInAllEndpoints]
        [IncludeInEndpoint("/app/admin/regions")]
        public List<RegionServerData> Servers { get; set; } = new List<RegionServerData>();

        [ExcludeInAllEndpoints]
        [IncludeInEndpoint("/app/admin/regions")]
        public RegionS3StorageServerData S3Server { get; set; } = new RegionS3StorageServerData();
    }
}
