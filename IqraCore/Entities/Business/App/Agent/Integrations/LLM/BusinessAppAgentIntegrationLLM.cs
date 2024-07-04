namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentIntegrationLLM
    {
        public string SelectedProvider { get; set; }
        public Dictionary<string, string> Configuration { get; set; }
        public Dictionary<string, List<Dictionary<string, string>>> ConfigurationWithLanguages { get; set; }
    }
}
