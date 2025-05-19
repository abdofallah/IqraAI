namespace IqraCore.Entities.Server
{
    public class BackendAppConfig
    {
        // General
        public string ServerId { get; set; } = string.Empty;
        public string RegionId { get; set; } = string.Empty;

        // Expected Calls at once
        public int ExpectedMaxConcurrentCalls { get; set; } = 50;

        // Network settings
        public string NetworkInterfaceName { get; set; } = string.Empty;
        public double MaxNetworkDownloadMbps { get; set; } = 100;
        public double MaxNetworkUploadMbps { get; set; } = 100;
    }
}
