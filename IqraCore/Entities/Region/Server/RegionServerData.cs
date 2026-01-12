using IqraCore.Entities.Helper.Server;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Region
{
    [BsonKnownTypes(typeof(RegionProxyServerData), typeof(RegionBackendServerData))]
    public class RegionServerData
    {
        [BsonId]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        public string Endpoint { get; set; } = string.Empty;
        public bool UseSSL { get; set; } = true;
        public string APIKey { get; set; } = string.Empty;
        public int SIPPort { get; set; } = 5060;

        public virtual ServerTypeEnum Type { get; set; } = ServerTypeEnum.Unknown;

        public bool IsDevelopmentServer { get; set; } = false;

        // Maintenance
        public DateTime? MaintenanceEnabledAt { get; set; } = null;
        public string? PrivateMaintenanceEnabledReason { get; set; } = null;
        public string? PublicMaintenanceEnabledReason { get; set; } = null;

        // Disabled
        public DateTime? DisabledAt { get; set; } = null;
        public string? PrivateDisabledReason { get; set; } = null;
        public string? PublicDisabledReason { get; set; } = null;
    }
}
