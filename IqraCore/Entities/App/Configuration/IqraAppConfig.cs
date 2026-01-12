using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.App.Configuration
{
    [BsonIgnoreExtraElements]
    public class IqraAppConfig
    {
        public bool AppInstalled { get; set; } = false;

        // The version of the schema/data currently in MongoDB
        public string InstalledVersion { get; set; } = string.Empty;
        public bool IsMigrationInProgress { get; set; } = false;

        public DateTime InstallationDate { get; set; } = DateTime.UtcNow;

        public DateTime? LastUpdateCheck { get; set; }

        public bool EnableExtraTelemetry { get; set; } = true;
        public string InstanceId { get; set; } = Guid.NewGuid().ToString();
    }
}
