namespace IqraCore.Entities.Business
{
    public class BusinessAppContextService
    {
        public long Id { get; set; }
        public Dictionary<string, string> Name { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> ShortDescription { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> LongDescription { get; set; } = new Dictionary<string, string>();
        public List<long> AvailableAtBranches { get; set; } = new List<long>();
        public List<long> RelatedProducts { get; set; } = new List<long>();
        public Dictionary<string, List<Dictionary<string, string>>> OtherInformation { get; set; } = new Dictionary<string, List<Dictionary<string, string>>>();
    }
}
