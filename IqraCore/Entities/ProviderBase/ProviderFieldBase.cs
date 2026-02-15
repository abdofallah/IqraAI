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
        public bool IsArray { get; set; } = false;

        // Hide/show field based on model selection
        public ProviderFieldModelCondition? ModelCondition { get; set; } = null;

        // Hide/show field based on other field value
        public List<ProviderFieldFieldCondition>? FieldConditions { get; set; } = null;

        // If type number or double number
        public double? MinNumberValue { get; set; } = null;
        public double? MaxNumberValue { get; set; } = null;
        // if double number
        public int? DecimalPlaces { get; set; } = null;

        // if type string
        public string? StringRegex { get; set; } = null;

        // if type is also array
        public int? MinArrayCount { get; set; } = null;
        public int? MaxArrayCount { get; set; } = null;
    }

    public class ProviderFieldOption
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsDefault { get; set; } = false;
    }

    public class ProviderFieldFieldCondition
    {
        public string FieldId { get; set; }
        public ProviderFieldFieldConitionType Type { get; set; }
        public ProviderFieldFieldConitionVisibility Visibility { get; set; }
        public string Value { get; set; }
    }

    public enum ProviderFieldFieldConitionType
    {
        Equal = 0,
        NotEqual = 1,
        Include = 2,
        Exclude = 3,
        GreaterThan = 4,
        LessThan = 5,
        GreaterThanOrEqual = 6,
        LessThanOrEqual = 7,
        StartsWith = 8,
        EndsWith = 9
    }

    public enum ProviderFieldFieldConitionVisibility
    {
        Visible = 0,
        Hidden = 1
    }

    public class ProviderFieldModelCondition
    {
        public ProviderFieldModelConitionType Type { get; set; } = ProviderFieldModelConitionType.Include;
        public List<string> Models { get; set; } = new List<string>();
    }

    public enum ProviderFieldModelConitionType
    {
        Include = 0,
        Exclude = 1
    }
}
