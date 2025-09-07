namespace IqraCore.Entities.Helper.Call.Outbound
{
    public class OutboundBulkCallRowData
    {
        public string? ToNumber { get; set; }
        public Dictionary<string, string>? DynamicVariables { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
        public string? OverrideAgentLanguageCode { get; set; }
        public List<string>? OverrideAgentTimezones { get; set; }
    }
}
