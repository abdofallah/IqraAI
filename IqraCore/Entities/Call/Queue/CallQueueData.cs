using IqraCore.Attributes;
using IqraCore.Entities.Helper.Call.Queue;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Call.Queue
{
    [BsonKnownTypes(typeof(InboundCallQueueData), typeof(OutboundCallQueueData))]
    public class CallQueueData
    {
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EnqueuedAt { get; set; } = null;
        public DateTime? ProcessingStartedAt { get; set; } = null;
        public DateTime? CompletedAt { get; set; } = null;

        public virtual CallQueueTypeEnum Type { get; set; } = CallQueueTypeEnum.Unknown;
        public CallQueueStatusEnum Status { get; set; } = CallQueueStatusEnum.WaitingForQueueing;

        public long BusinessId { get; set; }
        public string? RegionId { get; set; } = null;

        public string? SessionId { get; set; } = null;

        public List<CallQueueLog> Logs { get; set; } = new List<CallQueueLog>();

        [ExcludeInAllEndpoints]
        public string? ProcessingServerId { get; set; } = null;

        [ExcludeInAllEndpoints]
        public Dictionary<string, string> ProviderMetadata { get; set; } = new Dictionary<string, string>();
    }
}
