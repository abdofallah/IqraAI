namespace IqraCore.Entities.Business
{
    public class BusinessAppAgent
    {
        public string Id { get; set; }
        public BusinessAppAgentGeneral General { get; set; } = new BusinessAppAgentGeneral();
        public BusinessAppAgentContext Context { get; set; } = new BusinessAppAgentContext();
        public BusinessAppAgentPersonality Personality { get; set; } = new BusinessAppAgentPersonality();
        public BusinessAppAgentUtterances Utterances { get; set; } = new BusinessAppAgentUtterances();
        public BusinessAppAgentInterruption Interruptions { get; set; } = new BusinessAppAgentInterruption();
        public List<BusinessAppAgentScript> Scripts { get; set; } = new List<BusinessAppAgentScript>(); 
        public BusinessAppAgentIntegrations Integrations { get; set; } = new BusinessAppAgentIntegrations();
        public BusinessAppAgentCache Cache { get; set; } = new BusinessAppAgentCache();
        public BusinessAppAgentSettings Settings { get; set; } = new BusinessAppAgentSettings();
    }
}
