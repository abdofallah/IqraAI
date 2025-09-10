namespace IqraCore.Entities.Business
{
    public class BusinessAppWebCampaignAgent
    {
        public string SelectedAgentId { get; set; } = string.Empty;
        public string OpeningScriptId { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public List<string> Timezones { get; set; } = new List<string>();
    }
}
