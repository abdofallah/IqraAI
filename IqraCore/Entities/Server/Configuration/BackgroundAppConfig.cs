namespace IqraCore.Entities.Server.Configuration
{
    public class BackgroundAppConfig
    {
        // Static Config
        public bool IsCloudVersion { get; set; }
        public string DefaultS3StorageRegionId { get; set; }

        // Security
        public string ApiKey { get; set; }
    }
}
