namespace IqraCore.Entities.Server.Metrics.Hardware
{
    public struct HardwareMetrics
    {
        public double CpuUsagePercent { get; set; }
        public double MemoryUsagePercent { get; set; }
        public double NetworkDownloadMbps { get; set; }
        public double NetworkUploadMbps { get; set; }
    }
}
