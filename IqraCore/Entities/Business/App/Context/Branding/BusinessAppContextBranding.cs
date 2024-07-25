using IqraCore.Attributes;

namespace IqraCore.Entities.Business
{
    public class BusinessAppContextBranding
    {
        [MultiLanguageProperty]
        public Dictionary<string, string> Name { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, string> Country { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, string> Email { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, string> Phone { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, string> Website { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, List<Dictionary<string, string>>> OtherInformation { get; set; } = new Dictionary<string, List<Dictionary<string, string>>>();
    }
}
