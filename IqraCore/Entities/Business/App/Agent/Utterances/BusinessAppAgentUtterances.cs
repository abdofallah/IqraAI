using IqraCore.Attributes;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentUtterances
    {
        public BusinessAppAgentOpeningType OpeningType { get; set; } = BusinessAppAgentOpeningType.AgentFirst;

        [MultiLanguageProperty]
        public Dictionary<string, string> GreetingMessage { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, List<string>> PhrasesBeforeReply { get; set; } = new Dictionary<string, List<string>>();
    }
}
