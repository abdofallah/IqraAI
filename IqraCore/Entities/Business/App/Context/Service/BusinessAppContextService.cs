using IqraCore.Attributes;

namespace IqraCore.Entities.Business
{
    public class BusinessAppContextService
    {
        public string Id { get; set; }

        [MultiLanguageProperty]
        public Dictionary<string, string> Name { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, string> ShortDescription { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, string> LongDescription { get; set; } = new Dictionary<string, string>();

        public List<string> AvailableAtBranches { get; set; } = new List<string>();
        public List<string> RelatedProducts { get; set; } = new List<string>();

        [MultiLanguageProperty]
        public Dictionary<string, Dictionary<string, string>> OtherInformation { get; set; } = new Dictionary<string, Dictionary<string, string>>();
    }
}
