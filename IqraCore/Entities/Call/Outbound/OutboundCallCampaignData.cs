using IqraCore.Models.Business.MakeCalls;

namespace IqraCore.Entities.Call.Outbound
{
    public class OutboundCallCampaignData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public MakeCallRequestDto CallRequestData { get; set; }

        public bool IsBulkCall { get; set; } = false;
        public List<string>? CallQueueIds { get; set; } = null;

        public List<string> ErrorLogs { get; set; } = new List<string>();
    }
}
