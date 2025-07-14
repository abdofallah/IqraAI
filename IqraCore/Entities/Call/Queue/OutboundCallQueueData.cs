using IqraCore.Entities.Helper.Call.Outbound;
using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Helper.Telephony;

namespace IqraCore.Entities.Call.Queue
{
    public class OutboundCallQueueData : CallQueueData
    {
        public override CallQueueTypeEnum Type { get; set; } = CallQueueTypeEnum.Outbound;

        public string CampaignId { get; set; } = string.Empty;

        public string CallingNumberId { get; set; } = string.Empty;
        public TelephonyProviderEnum CallingNumberProvider { get; set; } = TelephonyProviderEnum.Unknown;
        public string? ProviderCallId { get; set; } = null;
        public string RecipientNumber { get; set; } = string.Empty;

        public DateTime ScheduledForDateTime { get; set; } = DateTime.UtcNow.AddHours(1);

        // Override Config Related
        public OutboundCallRetryData CallRetryOnDeclineData { get; set; } = new();
        public OutboundCallRetryData CallRetryOnMissedData { get; set; } = new();
        public Dictionary<string, string>? DynamicVariables { get; set; } = null;
        public string AgentId { get; set; } = string.Empty;
        public string AgentScriptId { get; set; } = string.Empty;
        public string AgentLanguageCode { get; set; } = string.Empty;
        public List<string> AgentTimeZone { get; set; } = new List<string>();
    }

    public class OutboundCallRetryData
    {
        public bool Enabled { get; set; } = false;
        public int? RetryCount { get; set; } = null;
        public int? RetryDelay { get; set; } = null;
        public OutboundCallRetryDelayUnitType? RetryUnit { get; set; } = null;

        public int? TimesTried { get; set; } = null;
        public DateTime? LastTried { get; set; } = null;
    }
}
