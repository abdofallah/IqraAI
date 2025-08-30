using IqraCore.Attributes;

namespace IqraCore.Entities.Business
{
    public class BusinessAppCampaignGeneral
    {
        public string Emoji { get; set; } = "🤖";

        [MultiLanguageProperty]
        public Dictionary<string, string> Name { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, string> Description { get; set; } = new Dictionary<string, string>();
    }
}
