namespace IqraCore.Entities.Business
{
    public class BusinessAppPostAnalysisExtraction
    {
        public List<BusinessAppPostAnalysisExtractionField> Fields { get; set; } = new List<BusinessAppPostAnalysisExtractionField>();
    }

    public class BusinessAppPostAnalysisExtractionField
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string KeyName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsRequired { get; set; } = false;
        public BusinessAppPostAnalysisExtractionFieldDataType DataType { get; set; } = BusinessAppPostAnalysisExtractionFieldDataType.String;

        public List<string> Options { get; set; } = new List<string>();

        public BusinessAppPostAnalysisExtractionFieldValidationRules Validation { get; set; } = new BusinessAppPostAnalysisExtractionFieldValidationRules();

        public List<BusinessAppPostAnalysisExtractionConditionalRule> ConditionalRules { get; set; } = new List<BusinessAppPostAnalysisExtractionConditionalRule>();
    }

    public enum BusinessAppPostAnalysisExtractionFieldDataType
    {
        String,
        Boolean,
        Number,
        DateTime,
        Enum
    }

    public class BusinessAppPostAnalysisExtractionFieldValidationRules
    {
        public string? Pattern { get; set; } = null;
        public int? MinLength { get; set; } = null;
        public int? MaxLength { get; set; } = null;
    }

    public class BusinessAppPostAnalysisExtractionConditionalRule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public BusinessAppPostAnalysisExtractionFieldCondition Condition { get; set; } = new BusinessAppPostAnalysisExtractionFieldCondition();
        public List<BusinessAppPostAnalysisExtraction> FieldsToExtract { get; set; } = new List<BusinessAppPostAnalysisExtraction>();
    }

    public class BusinessAppPostAnalysisExtractionFieldCondition
    {
        public BusinessAppPostAnalysisExtractionConditionOperator Operator { get; set; } = BusinessAppPostAnalysisExtractionConditionOperator.Equals;
        public string Value { get; set; } = string.Empty;
    }

    public enum BusinessAppPostAnalysisExtractionConditionOperator
    {
        Equals,
        NotEquals,
        Contains,
        GreaterThan, // For numbers/datetime
        LessThan     // For numbers/datetime
    }
}