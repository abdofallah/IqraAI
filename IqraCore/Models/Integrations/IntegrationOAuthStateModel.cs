namespace IqraCore.Models.Integrations
{
    public class IntegrationOAuthStateModel
    {
        public long BusinessId { get; set; }
        public string IntegrationId { get; set; } = string.Empty;
        public string IntegrationType { get; set; } = string.Empty;
        public long IssuedAtUnix { get; set; }
    }
}
