using IqraCore.Attributes;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentPersonality
    {
        [MultiLanguageProperty]
        public Dictionary<string, string> Name { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, List<string>> Capabilities { get; set; } = new Dictionary<string, List<string>>();

        [MultiLanguageProperty]
        public Dictionary<string, List<string>> Ethics { get; set; } = new Dictionary<string, List<string>>();

        [MultiLanguageProperty]
        public Dictionary<string, List<string>> Tone { get; set; } = new Dictionary<string, List<string>>();
    }
}
