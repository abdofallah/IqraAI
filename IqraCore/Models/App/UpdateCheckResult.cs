using IqraCore.Entities.App.Update;

namespace IqraCore.Models.App
{
    public class UpdateCheckResult
    {
        public bool IsUpdateAvailable { get; set; } = false;
        public string CurrentVersion { get; set; } = string.Empty;
        public string LatestVersion { get; set; } = string.Empty;
        public List<SecurityNotice> SecurityWarnings { get; set; } = new();
        public string? ChangelogUrl { get; set; }
    }
}