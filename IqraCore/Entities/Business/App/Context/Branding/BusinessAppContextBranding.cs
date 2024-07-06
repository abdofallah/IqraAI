namespace IqraCore.Entities.Business
{
    public class BusinessAppContextBranding
    {
        public Dictionary<string, string> Name { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Country { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Email { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Phone { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Website { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, List<Dictionary<string, string>>> OtherInformation { get; set; } = new Dictionary<string, List<Dictionary<string, string>>>();
    }
}
