namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessAppAgent
    {
        public long Id { get; set; }
        public BusinessAppAgentGeneral General { get; set; }
        public BusinessAppAgentContext Context { get; set; }
        public BusinessAppAgentPersonality Personality { get; set; }
        public BusinessAppAgentUtterances Utterances { get; set; }
        public List<BusinessAppAgentScript> Scripts { get; set; }
        public BusinessAppAgentIntegrations Integrations { get; set; }
        public BusinessAppAgentCache Cache { get; set; }
        public BusinessAppAgentSettings Settings { get; set; }
    }
}
