namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessAppAgent
    {
        public long Id { get; set; }
        public BusinessAppAgentGeneral General { get; set; }
        public BusinessAppAgentContext Context { get; set; }
        public BusinessAppAgentPersonality Personality { get; set; }
        public BusinessAppAgentUtterances Utterances { get; set; }
    }
}
