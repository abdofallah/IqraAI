using IqraCore.Attributes;
using IqraCore.Entities.S3Storage;
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

        // S3 Related
        [ExcludeInAllEndpoints]
        [IncludeInEndpoint("/app/admin/regions")]
        public bool UseDefaultS3 { get; set; } = true;

        [ExcludeInAllEndpoints]
        [IncludeInEndpoint("/app/admin/regions")]
        public S3StorageConfigData? S3Server { get; set; } = null;
    }
}
