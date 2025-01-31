namespace IqraCore.Entities.ProviderBase
{
    public class ProviderFieldBase
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Tooltip { get; set; } = string.Empty;
        public string Placeholder { get; set; } = string.Empty;
        public string DefaultValue { get; set; } = string.Empty;
        public List<ProviderFieldOption>? Options { get; set; } = null;
        public bool Required { get; set; } = false;
        public bool IsEncrypted { get; set; } = false;
    }

    public class ProviderFieldOption
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsDefault { get; set; } = false;
    }
}
