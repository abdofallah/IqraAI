namespace IqraCore.Entities.Region
{
    public class RegionS3StorageServerData
    {
        public string Endpoint { get; set; } = string.Empty;
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public bool UseSSL { get; set; } = false;
        public DateTime? DisabledAt = null;
    }
}
