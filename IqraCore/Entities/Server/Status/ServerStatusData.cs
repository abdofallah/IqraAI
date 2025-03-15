using IqraCore.Entities.Helper.Server;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Server
{
    public class ServerStatusData
    {
        [BsonId]
        public string ServerId { get; set; } = string.Empty;

        public string RegionId { get; set; } = string.Empty;
        public ServerTypeEnum Type { get; set; } = ServerTypeEnum.Backend;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public bool MaintenanceMode { get; set; } = false;
        public DateTime? MaintenanceModeStartedAt { get; set; } = null;

        public int CurrentActiveCallsCount { get; set; } = 0;
        public int MaxConcurrentCallsCount { get; set; } = 100;
        public int QueuedCallsCount { get; set; } = 0;

        public double CpuUsagePercent { get; set; } = 0;
        public double MemoryUsagePercent { get; set; } = 0;
        public double NetworkUsageMbps { get; set; } = 0;

        public ServerLoadStatusEnum LoadStatus
        {
            get
            {
                double loadPercent = (double)CurrentActiveCallsCount / MaxConcurrentCallsCount * 100;

                if (loadPercent >= 90) return ServerLoadStatusEnum.Critical;
                if (loadPercent >= 75) return ServerLoadStatusEnum.Heavy;
                if (loadPercent >= 50) return ServerLoadStatusEnum.Moderate;
                if (loadPercent >= 25) return ServerLoadStatusEnum.Light;
                return ServerLoadStatusEnum.Minimal;
            }
        }

        public ServerStatusData Clone()
        {
            return new ServerStatusData
            {
                ServerId = ServerId,
                RegionId = RegionId,
                Type = Type,
                LastUpdated = LastUpdated,
                MaintenanceMode = MaintenanceMode,
                MaintenanceModeStartedAt = MaintenanceModeStartedAt,
                CurrentActiveCallsCount = CurrentActiveCallsCount,
                MaxConcurrentCallsCount = MaxConcurrentCallsCount,
                QueuedCallsCount = QueuedCallsCount,
                CpuUsagePercent = CpuUsagePercent,
                MemoryUsagePercent = MemoryUsagePercent,
                NetworkUsageMbps = NetworkUsageMbps
            };
        }
    }
}
