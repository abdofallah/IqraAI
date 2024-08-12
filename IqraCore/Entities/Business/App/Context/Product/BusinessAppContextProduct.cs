using IqraCore.Attributes;

namespace IqraCore.Entities.Business
{
    public class BusinessAppContextProduct
    {
        public long Id { get; set; }

        [MultiLanguageProperty]
        public Dictionary<string, string> Name { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, string> ShortDescription { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, string> LongDescription { get; set; } = new Dictionary<string, string>();

        public List<long> AvailableAtBranches { get; set; } = new List<long>();

        [MultiLanguageProperty]
        public Dictionary<string, List<Dictionary<string, string>>> OtherInformation { get; set; } = new Dictionary<string, List<Dictionary<string, string>>>();
    }
}
