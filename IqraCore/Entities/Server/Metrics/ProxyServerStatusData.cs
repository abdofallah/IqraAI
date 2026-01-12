using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Server.Metrics
{
    [BsonIgnoreExtraElements]
    public class ProxyServerStatusData : ServerStatusData
    {
        public string RegionId { get; set; } = string.Empty;

        public int CurrentOutboundMarkedQueues { get; set; } = 0;
        public int CurrentOutboundProcessingMarkedQueues { get; set; } = 0;
        public int CurrentOutboundProcessedMarkedQueues { get; set; } = 0;
    }
}
