namespace IqraCore.Entities.LLM
{
    public class LLMProviderUserIntegrationFieldData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // text, number, select, models
        public string Tooltip { get; set; } = "";
        public string Placeholder { get; set; } = "";
        public string DefaultValue { get; set; } = "";

        public List<LLMProviderUserIntegrationFieldOption>? Options { get; set; } = null; // for select type

        public bool Required { get; set; } = false;
        public bool IsEncrypted { get; set; } = false;
    }

    public class LLMProviderUserIntegrationFieldOption
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsDefault { get; set; } = false;
    }
}
