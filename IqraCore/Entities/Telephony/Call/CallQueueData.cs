using IqraCore.Entities.Helper.Telephony;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Telephony.Call
{
    public class CallQueueData
    {
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public DateTime EnqueuedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessingStartedAt { get; set; } = null;
        public DateTime? CompletedAt { get; set; } = null;

        public CallQueueStatusEnum Status { get; set; } = CallQueueStatusEnum.Queued;

        public long BusinessId { get; set; }
        public string RegionId { get; set; } = string.Empty;
        public string NumberId { get; set; } = string.Empty;
        public string RouteId { get; set; } = string.Empty;

        public TelephonyProviderEnum Provider { get; set; } = TelephonyProviderEnum.Unknown;
        public string ProviderCallId { get; set; } = string.Empty;
        public string CallerNumber { get; set; } = string.Empty;

        public int Priority { get; set; } = 0;
        public bool IsOutbound { get; set; } = false;

        public string ProcessingServerId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;

        public Dictionary<string, string> ProviderMetadata { get; set; } = new Dictionary<string, string>();
    }
}
