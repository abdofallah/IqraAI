namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentIntegrations
    {
        public List<BusinessAppAgentIntegrationData> STT { get; set; } = new List<BusinessAppAgentIntegrationData>();
        public List<BusinessAppAgentIntegrationData> TTS { get; set; } = new List<BusinessAppAgentIntegrationData>();
        public List<BusinessAppAgentIntegrationData> LLM { get; set; } = new List<BusinessAppAgentIntegrationData>();
    }
}
