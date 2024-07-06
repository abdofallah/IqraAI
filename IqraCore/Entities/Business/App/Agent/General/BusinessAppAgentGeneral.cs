namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentGeneral
    {
        public string Emoji { get; set; } = "🤖";
        public Dictionary<string, string> Name { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Description { get; set; } = new Dictionary<string, string>();
    }
}
