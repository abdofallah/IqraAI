using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Server.Metrics
{
    [BsonIgnoreExtraElements]
    public class BackendServerStatusData : ServerStatusData
    {
        public string RegionId { get; set; } = string.Empty;

        public int MaxConcurrentCallsCount { get; set; } = 0;

        public int CurrentActiveTelephonySessionCount { get; set; } = 0;
        public int CurrentActiveWebSessionCount { get; set; } = 0;
    }
}
