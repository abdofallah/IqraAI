namespace IqraCore.Entities.Business.Integration
{
    public class BusinessAppIntegrationTTS
    {
        public string Id { get; set; }
        public string? SelectedProvider { get; set; } = null;
        public Dictionary<string, string> Configuration { get; set; } = new Dictionary<string, string>();

        // TODO create subtypes/abstract classes of config as multi lang attribute will fail here
        //public Dictionary<string, List<Dictionary<string, string>>> ConfigurationWithLanguages { get; set; } = new Dictionary<string, List<Dictionary<string, string>>>();
    }
}
