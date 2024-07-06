namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentIntegrationTTS
    {
        public string? SelectedProvider { get; set; } = null;
        public Dictionary<string, string> Configuration { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, List<Dictionary<string, string>>> ConfigurationWithLanguages { get; set; } = new Dictionary<string, List<Dictionary<string, string>>>();
    }
}
