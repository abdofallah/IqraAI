namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentScriptGeneral
    {
        public Dictionary<string, string> Name { get; set; }
        public Dictionary<string, string> Description { get; set; }
        public Dictionary<string, List<string>> Conditions { get; set; }
    }
}
