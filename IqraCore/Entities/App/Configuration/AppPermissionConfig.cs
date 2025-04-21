namespace IqraCore.Entities.App.Configuration
{
    public class AppPermissionConfig
    {
        // Maintenance
        public DateTime? MaintenanceEnabledAt { get; set; } = null;
        public string? PrivateMaintenanceEnabledReason { get; set; } = null;
        public string? PublicMaintenanceEnabledReason { get; set; } = null;

        // Authentication
        public DateTime? RegisterationDisabledAt { get; set; } = null;
        public string? PrivateRegisterationDisabledReason { get; set; } = null;
        public string? PublicRegisterationDisabledReason { get; set; } = null;

        public DateTime? LoginDisabledAt { get; set; } = null;
        public string? PrivateLoginDisabledReason { get; set; } = null;
        public string? PublicLoginDisabledReason { get; set; } = null;
    }
}
