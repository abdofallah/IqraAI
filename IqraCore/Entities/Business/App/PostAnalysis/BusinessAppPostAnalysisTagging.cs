using MongoDB.Bson;

namespace IqraCore.Entities.Business
{
    public class BusinessAppPostAnalysisTagging
    {
        public bool IsActive { get; set; } = true;
        public List<BusinessAppPostAnalysisTagDefinition> Tags { get; set; } = new List<BusinessAppPostAnalysisTagDefinition>();
    }

    public class BusinessAppPostAnalysisTagDefinition
    {
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public BusinessAppPostAnalysisTagRules Rules { get; set; } = new BusinessAppPostAnalysisTagRules();

        public List<BusinessAppPostAnalysisTagDefinition> SubTags { get; set; } = new List<BusinessAppPostAnalysisTagDefinition>();
    }

    public class BusinessAppPostAnalysisTagRules
    {
        public bool AllowMultiple { get; set; } = false;
        public bool IsRequired { get; set; } = false;
    }
}