using System.Text.Json.Serialization;

namespace IqraCore.Entities.Helper.Call.Outbound
{
    public class OutboundBulkCallRowData
    {
        public string? FromNumberId { get; set; }
        public string? ToNumber { get; set; }
        public Dictionary<string, string>? DynamicVariables { get; set; }
        public OutboundBulkCallRowDataRetryData? OverrideRetryCallDeclinedData { get; set; }
        public OutboundBulkCallRowDataRetryData? OverrideRetryCallMissedData { get; set; }
        public string? OverrideAgentId { get; set; }
        public string? OverrideSelectedAgentScriptId { get; set; }
        public string? OverrideAgentLanguageCode { get; set; }
        public List<string>? OverrideAgentTimezones { get; set; }
    }

    public class OutboundBulkCallRowDataRetryData
    {
        public bool? Enabled { get; set; }

        // if enabled
        public int? Count { get; set; }
        public int? Delay { get; set; }
        public OutboundCallRetryDelayUnitType? Unit { get; set; }
    }
}
