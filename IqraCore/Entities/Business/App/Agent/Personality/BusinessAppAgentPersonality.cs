namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentPersonality
    {
        public Dictionary<string, string> Name { get; set; }
        public Dictionary<string, List<string>> Capabilities { get; set; }
        public Dictionary<string, List<string>> Ethics { get; set; }
        public Dictionary<string, List<string>> Tone { get; set; }
    }
}
