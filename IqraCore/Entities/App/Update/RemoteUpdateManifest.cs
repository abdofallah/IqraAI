namespace IqraCore.Entities.App.Update
{
    public class RemoteUpdateManifest
    {
        public string LatestVersion { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public string ChangelogUrl { get; set; } = string.Empty;

        public List<SecurityNotice> CriticalSecurityNotices { get; set; } = new();

        // Key = Version Number (e.g. "1.5.0")
        public Dictionary<string, MigrationInfo> Migrations { get; set; } = new();
    }

    public class SecurityNotice
    {
        public string MinVersion { get; set; } = string.Empty;
        public string MaxVersion { get; set; } = string.Empty;
        public string Severity { get; set; } = "INFO"; // INFO, WARNING, CRITICAL
        public string Message { get; set; } = string.Empty;
    }

    public class MigrationInfo
    {
        public bool RequiresDowntime { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}