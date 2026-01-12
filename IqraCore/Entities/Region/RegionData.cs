using IqraCore.Attributes;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Region
{
    public class RegionData
    {
        [BsonId]
        public string RegionId { get; set; } = "";
        public string CountryCode { get; set; } = string.Empty;
        public string RegionName { get; set; } = string.Empty;

        // Maintenance
        public DateTime? MaintenanceEnabledAt { get; set; } = null;
        public string? PrivateMaintenanceEnabledReason { get; set; } = null;
        public string? PublicMaintenanceEnabledReason { get; set; } = null;

        // Disabled
        public DateTime? DisabledAt { get; set; } = null;
        public string? PrivateDisabledReason { get; set; } = null;
        public string? PublicDisabledReason { get; set; } = null;

        [ExcludeInAllEndpoints]
        [IncludeInEndpoint("/app/admin/regions")]
        public List<RegionServerData> Servers { get; set; } = new List<RegionServerData>();

        [ExcludeInAllEndpoints]
        [IncludeInEndpoint("/app/admin/regions")]
        public RegionS3StorageServerData S3Server { get; set; } = new RegionS3StorageServerData();
    }
}
