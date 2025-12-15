using IqraCore.Models.Business.MakeCalls;
using MongoDB.Bson;

namespace IqraCore.Entities.Call.Outbound
{
    public class OutboundCallQueueGroupData
    {
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public long BusinessId { get; set; }

        public MakeCallRequestDto CallRequestData { get; set; }

        public bool IsBulkCall { get; set; } = false;

        public List<string?> CallQueueIds { get; set; } = new List<string?>();
        public List<string> ErrorLogs { get; set; } = new List<string>();
    }
}
