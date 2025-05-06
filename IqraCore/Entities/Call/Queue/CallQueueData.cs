using IqraCore.Attributes;
using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Helper.Telephony;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Call.Queue
{
    [BsonKnownTypes(typeof(InboundCallQueueData), typeof(OutboundCallQueueData))]
    public class CallQueueData
    {
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public DateTime EnqueuedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessingStartedAt { get; set; } = null;
        public DateTime? CompletedAt { get; set; } = null;

        public CallQueueTypeEnum Type { get; set; } = CallQueueTypeEnum.Unknown;
        public CallQueueStatusEnum Status { get; set; } = CallQueueStatusEnum.Queued;

        public long BusinessId { get; set; }
        public string RegionId { get; set; } = string.Empty;

        public string SessionId { get; set; } = string.Empty;

        public List<CallQueueLog> Logs { get; set; } = new List<CallQueueLog>();
        
        [ExcludeInAllEndpoints]
        public string ProcessingServerId { get; set; } = string.Empty;

        [ExcludeInAllEndpoints]
        public Dictionary<string, string> ProviderMetadata { get; set; } = new Dictionary<string, string>();
    }
}
