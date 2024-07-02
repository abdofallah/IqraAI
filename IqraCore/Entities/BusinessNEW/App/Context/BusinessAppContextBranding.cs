namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessAppContextBranding
    {
        public Dictionary<string, string> Name { get; set; }
        public Dictionary<string, string> Country { get; set; }
        public Dictionary<string, string> Email { get; set; }
        public Dictionary<string, string> Phone { get; set; }
        public Dictionary<string, string> Website { get; set; }
        public Dictionary<string, List<Dictionary<string, string>>> OtherInformation { get; set; }
    }
}
