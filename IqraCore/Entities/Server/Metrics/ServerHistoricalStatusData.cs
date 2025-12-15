using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Server
{
    public class ServerHistoricalStatusData
    {
        [BsonId]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public string ServerId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public int ActiveCalls { get; set; } = 0;
        public int QueuedCalls { get; set; } = 0;

        public double CpuUsagePercent { get; set; } = 0;
        public double MemoryUsagePercent { get; set; } = 0;
        public double NetworkDownloadMbps { get; set; } = 0;
        public double NetworkUploadMbps { get; set; } = 0;
    }
}
