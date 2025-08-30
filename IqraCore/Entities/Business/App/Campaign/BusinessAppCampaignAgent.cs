namespace IqraCore.Entities.Business
{
    public class BusinessAppCampaignAgent
    {
        public string AgentId { get; set; }
        public string AgentScriptId { get; set; }
        public string DefaultLangauge { get; set; }
        public string DefaultTimeZone { get; set; }
        public bool IncludeFromNumberInContext { get; set; }
        public bool IncludeToNumberInContext { get; set; }
    }
}
