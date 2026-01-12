using IqraCore.Entities.App.Enum;
using IqraCore.Entities.Node.Enum;
using IqraCore.Entities.Server.Metrics;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Server
{
    [BsonIgnoreExtraElements]
    [BsonKnownTypes(typeof(ProxyServerStatusData), typeof(BackendServerStatusData))]
    public class ServerStatusData : ICloneable
    {
        public string NodeId { get; set; } = string.Empty;

        public AppNodeTypeEnum Type { get; set; } = AppNodeTypeEnum.Unknown;

        public NodeRuntimeStatus RuntimeStatus { get; set; } = NodeRuntimeStatus.Starting;
        public string RuntimeStatusReason { get; set; } = "Node Starting!";

        public string Version { get; set; } = string.Empty;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public double CpuUsagePercent { get; set; } = 0;
        public double MemoryUsagePercent { get; set; } = 0;
        public double NetworkDownloadMbps { get; set; } = 0;
        public double NetworkUploadMbps { get; set; } = 0;

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
