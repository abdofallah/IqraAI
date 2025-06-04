using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.App.Configuration
{
    [BsonIgnoreExtraElements]
    public class AppPermissionConfig
    {
        // Maintenance
        public DateTime? MaintenanceEnabledAt { get; set; } = DateTime.UtcNow;
        public string? PrivateMaintenanceEnabledReason { get; set; } = "Did I just get reinitalized?";
        public string? PublicMaintenanceEnabledReason { get; set; } = "Uh! We seem to be under maintenance.";

        // Authentication
        public DateTime? RegisterationDisabledAt { get; set; } = DateTime.UtcNow;
        public string? PrivateRegisterationDisabledReason { get; set; } = "Did I just get reinitalized?";
        public string? PublicRegisterationDisabledReason { get; set; } = "Ah! We seem to be under maintenance.";

        public DateTime? LoginDisabledAt { get; set; } = DateTime.UtcNow;
        public string? PrivateLoginDisabledReason { get; set; } = "Did I just get reinitalized?";
        public string? PublicLoginDisabledReason { get; set; } = "Oh! We seem to be under maintenance.";
    }
}
