namespace IqraCore.Models.Server
{
    public class BackendOutboundCallRequest
    {
        public string QueueId { get; set; }
        public long BusinessId { get; set; }
        public string RegionId { get; set; }
        public string CampaignId { get; set; }
        public string CallingNumberId { get; set; }
        public string RecipientNumber { get; set; }
        public Dictionary<string, string>? DynamicVariables { get; set; }
        public string AgentId { get; set; }
        public string AgentScriptId { get; set; }
        public string AgentLanguageCode { get; set; }
        public List<string> AgentTimeZone { get; set; }
    }

}
