namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentIntegrations
    {
        public BusinessAppAgentIntegrationSTT STT { get; set; } = new BusinessAppAgentIntegrationSTT();
        public BusinessAppAgentIntegrationTTS TTS { get; set; } = new BusinessAppAgentIntegrationTTS();
        public BusinessAppAgentIntegrationLLM LLM { get; set; } = new BusinessAppAgentIntegrationLLM();
    }
}
