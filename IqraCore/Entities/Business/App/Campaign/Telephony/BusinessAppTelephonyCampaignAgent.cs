namespace IqraCore.Entities.Business
{
    public class BusinessAppTelephonyCampaignAgent
    {
        public string SelectedAgentId { get; set; } = string.Empty;
        public string OpeningScriptId { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public List<string> Timezones { get; set; } = new List<string>();
        public bool FromNumberInContext { get; set; } = true;
        public bool ToNumberInContext { get; set; } = true;
    }
}
