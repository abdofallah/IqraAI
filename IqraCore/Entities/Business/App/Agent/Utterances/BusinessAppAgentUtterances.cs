namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentUtterances
    {
        public BusinessAppAgentOpeningType OpeningType { get; set; }
        public Dictionary<string, string> GreetingMessage { get; set; }
        public Dictionary<string, List<string>> PhrasesBeforeReply { get; set; }
    }
}
