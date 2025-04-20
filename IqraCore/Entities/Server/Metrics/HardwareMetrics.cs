namespace IqraCore.Entities.Server.Metrics
{
    public struct HardwareMetrics
    {
        public double CpuUsagePercent { get; set; }
        public double MemoryUsagePercent { get; set; }
        public double NetworkDownloadMbps { get; set; }
        public double NetworkUploadMbps { get; set; }
    }
}
