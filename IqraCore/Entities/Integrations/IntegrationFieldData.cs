namespace IqraCore.Entities.Integrations
{
    public class IntegrationFieldData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // text, number, select
        public string Tooltip { get; set; } = "";
        public string Placeholder { get; set; } = "";
        public string DefaultValue { get; set; } = "";

        public List<IntegrationFieldOption>? Options { get; set; } // for select type

        public bool Required { get; set; } = false;
        public bool IsEncrypted { get; set; } = false;
    }

    public class IntegrationFieldOption
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsDefault { get; set; } = false;
    }
}
