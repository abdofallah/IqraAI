namespace IqraCore.Entities.Server.Configuration
{
    public class FrontendAppConfig
    {
        public string DefaultS3StorageRegionId { get; set; } = null!;
        public bool IsCloudVersion { get; set; }
    }
}
