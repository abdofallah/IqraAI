using IqraCore.Attributes;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentUtterances
    {
        public BusinessAppAgentOpeningType OpeningType { get; set; } = BusinessAppAgentOpeningType.AgentFirst;

        [MultiLanguageProperty]
        public Dictionary<string, string> GreetingMessage { get; set; } = new Dictionary<string, string>();
    }
}
