using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Helper.Telephony;

namespace IqraCore.Entities.Call.Queue
{
    public class OutboundCallQueueData : CallQueueData
    {
        public override CallQueueTypeEnum Type { get; set; } = CallQueueTypeEnum.Outbound;

        public string CampaignId { get; set; } = string.Empty;
        public string QueueGroupId { get; set; } = string.Empty;

        public string CallingNumberId { get; set; } = string.Empty;
        public TelephonyProviderEnum CallingNumberProvider { get; set; } = TelephonyProviderEnum.Unknown;
        public string? ProviderCallId { get; set; } = null;
        public string RecipientNumber { get; set; } = string.Empty;

        public DateTime ScheduledForDateTime { get; set; } = DateTime.UtcNow;
        public DateTime MaxScheduleForDateTime { get; set; } = DateTime.UtcNow.AddHours(1);

        // Override Config Related
        public Dictionary<string, string> DynamicVariables { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}
