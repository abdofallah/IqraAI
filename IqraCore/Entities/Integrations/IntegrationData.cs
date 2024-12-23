namespace IqraCore.Entities.Integrations
{
    public class IntegrationData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? DisabledAt { get; set; } = null;
        public string? Logo { get; set; } = null;
        public List<string> Type { get; set; } = new List<string>();
        public List<IntegrationFieldData> Fields { get; set; } = new List<IntegrationFieldData>();
        public IntegrationHelpData Help { get; set; } = new IntegrationHelpData();
    }
}
