namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentInterruptionVerification
    {
        public bool Enabled { get; set; } = false;
        public bool UseAgentLLM { get; set; } = true;
        public BusinessAppAgentIntegrationData? LLMIntegration { get; set; } = null;
    }
}