using MongoDB.Bson;

namespace IqraCore.Entities.Business
{
    public class BusinessAppPostAnalysisExtraction
    {
        public bool IsActive { get; set; } = true;
        public List<BusinessAppPostAnalysisExtractionField> Fields { get; set; } = new List<BusinessAppPostAnalysisExtractionField>();
    }

    public class BusinessAppPostAnalysisExtractionField
    {
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public string KeyName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsRequired { get; set; } = false;
        public bool IsEmptyOrNullAllowed { get; set; } = false;
        public BusinessAppPostAnalysisExtractionFieldDataType DataType { get; set; } = BusinessAppPostAnalysisExtractionFieldDataType.String;

        // For enum data type
        public List<string>? Options { get; set; } = null;

        public BusinessAppPostAnalysisExtractionFieldValidationRules Validation { get; set; } = new BusinessAppPostAnalysisExtractionFieldValidationRules();

        public List<BusinessAppPostAnalysisExtractionConditionalRule> ConditionalRules { get; set; } = new List<BusinessAppPostAnalysisExtractionConditionalRule>();
    }

    public enum BusinessAppPostAnalysisExtractionFieldDataType
    {
        String = 0,
        Boolean = 1,
        Number = 2,
        DateTime = 3,
        Enum = 4
    }

    public class BusinessAppPostAnalysisExtractionFieldValidationRules
    {
        public string? Pattern { get; set; } = null;
        public int? Min { get; set; } = null;
        public int? Max { get; set; } = null;
    }

    public class BusinessAppPostAnalysisExtractionConditionalRule
    {
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public BusinessAppPostAnalysisExtractionFieldCondition Condition { get; set; } = new BusinessAppPostAnalysisExtractionFieldCondition();
        public List<BusinessAppPostAnalysisExtractionField> FieldsToExtract { get; set; } = new List<BusinessAppPostAnalysisExtractionField>();
    }

    public class BusinessAppPostAnalysisExtractionFieldCondition
    {
        public BusinessAppPostAnalysisExtractionConditionOperator Operator { get; set; } = BusinessAppPostAnalysisExtractionConditionOperator.Equals;
        public string Value { get; set; } = string.Empty;
    }

    public enum BusinessAppPostAnalysisExtractionConditionOperator
    {
        Equals = 0,
        NotEquals = 1,
        Contains = 2,
        // For numbers/datetime
        GreaterThan = 3,
        GreaterThanOrEqual = 4,
        LessThan = 5,
        LessThanOrEqual = 6
    }
}