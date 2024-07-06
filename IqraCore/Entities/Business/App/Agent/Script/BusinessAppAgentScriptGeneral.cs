namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentScriptGeneral
    {
        public Dictionary<string, string> Name { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Description { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, List<string>> Conditions { get; set; } = new Dictionary<string, List<string>>();
    }
}
