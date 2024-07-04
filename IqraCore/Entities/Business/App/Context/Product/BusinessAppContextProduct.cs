namespace IqraCore.Entities.Business
{
    public class BusinessAppContextProduct
    {
        public long Id { get; set; }
        public Dictionary<string, string> Name { get; set; }
        public Dictionary<string, string> ShortDescription { get; set; }
        public Dictionary<string, string> LongDescription { get; set; }
        public List<long> AvailableAtBranches { get; set; }
        public Dictionary<string, List<Dictionary<string, string>>> OtherInformation { get; set; }
    }
}
