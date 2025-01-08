using IqraCore.Attributes;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentIntegrations
    {
        [MultiLanguageProperty]
        public Dictionary<string, BusinessAppAgentIntegrationData> STT { get; set; } = new Dictionary<string, BusinessAppAgentIntegrationData>();

        [MultiLanguageProperty]
        public Dictionary<string, BusinessAppAgentIntegrationData> TTS { get; set; } = new Dictionary<string, BusinessAppAgentIntegrationData>();

        [MultiLanguageProperty]
        public Dictionary<string, BusinessAppAgentIntegrationData> LLM { get; set; } = new Dictionary<string, BusinessAppAgentIntegrationData>();
    }
}
