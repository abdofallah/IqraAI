namespace IqraCore.Entities.Server.Configuration
{
    public class ProxyAppConfig
    {
        // Static Config
        public bool IsCloudVersion { get; set; }
        public string ServerId { get; set; } = null!;
        public string RegionId { get; set; } = null!;
        public ProxyAppOutboundProcessingConfig OutboundProcessing { get; set; } = new();

        // Dynamic Config
        public string ServerEndpoint { get; set; } = null!;
        public int SIPPort { get; set; }
    }

    public class ProxyAppOutboundProcessingConfig
    {
        public int PollingIntervalSeconds { get; set; }
        public int DbFetchBatchSize { get; set; }
        public int ProcessingBatchSize { get; set; }
        public int ScheduleWindowMinutes { get; set; }
    }
}
