namespace IqraCore.Models.FlowApp.Integration
{
    public class BusinessAppIntegrationDecryptedModel
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string FriendlyName { get; set; } = string.Empty;
        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> DecryptedFields { get; set; } = new Dictionary<string, string>();
    }
}
