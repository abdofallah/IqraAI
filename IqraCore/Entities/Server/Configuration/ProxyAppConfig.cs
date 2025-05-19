namespace IqraCore.Entities.Server.Configuration
{
    public class ProxyAppConfig
    {
        public string RegionId { get; set; }
        public string Identity { get; set; }
        public ProxyAppOutboundProcessingConfig OutboundProcessing { get; set; }
    }

    public class ProxyAppOutboundProcessingConfig
    {
        public int PollingIntervalSeconds { get; set; }
        public int DbFetchBatchSize { get; set; }
        public int ProcessingBatchSize { get; set; }
        public int ScheduleWindowMinutes { get; set; }
    }
}
