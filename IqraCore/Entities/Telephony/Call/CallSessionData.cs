using IqraCore.Entities.Helper.Telephony;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Telephony.Call
{
    public class CallSessionData
    {
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string QueueId { get; set; } = string.Empty;

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EndedAt { get; set; } = null;

        public CallSessionStatusEnum Status { get; set; } = CallSessionStatusEnum.Initializing;
        public string CurrentNodeId { get; set; } = string.Empty;

        public long ConversationId { get; set; } = 0;
        public long BusinessId { get; set; }
        public string RegionId { get; set; } = string.Empty;

        public string ProcessingServer { get; set; } = string.Empty;

        public List<CallSessionLogEntry> Logs { get; set; } = new List<CallSessionLogEntry>();
        public CallSessionMetrics Metrics { get; set; } = new CallSessionMetrics();
    }
}
