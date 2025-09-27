namespace IqraCore.Entities.Business
{
    public class BusinessAppPostAnalysisTagging
    {
        public List<BusinessAppPostAnalysisTagSet> TagSets { get; set; } = new List<BusinessAppPostAnalysisTagSet>();
    }

    public class BusinessAppPostAnalysisTagSet
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public BusinessAppPostAnalysisTagSetRules Rules { get; set; } = new BusinessAppPostAnalysisTagSetRules();

        public List<BusinessAppPostAnalysisTagDefinition> Tags { get; set; } = new List<BusinessAppPostAnalysisTagDefinition>();
    }

    public class BusinessAppPostAnalysisTagSetRules
    {
        public bool AllowMultiple { get; set; } = false;
        public bool IsRequired { get; set; } = true;
    }

    public class BusinessAppPostAnalysisTagDefinition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string? ParentTagId { get; set; } = null;
    }
}