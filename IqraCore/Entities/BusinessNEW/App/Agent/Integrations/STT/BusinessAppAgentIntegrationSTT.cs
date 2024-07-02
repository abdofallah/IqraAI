namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessAppAgentIntegrationSTT
    {
        public string SelectedProvider { get; set; }
        public Dictionary<string, string> Configuration { get; set; }
        public Dictionary<string, List<Dictionary<string, string>>> ConfigurationWithLanguages { get; set; }
    }
}
