namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentUtterances
    {
        public BusinessAppAgentOpeningType OpeningType { get; set; } = BusinessAppAgentOpeningType.AgentFirst;
        public Dictionary<string, string> GreetingMessage { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, List<string>> PhrasesBeforeReply { get; set; } = new Dictionary<string, List<string>>();
    }
}
