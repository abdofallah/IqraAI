using IqraCore.Attributes;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentIntegrations
    {
        [MultiLanguageProperty]
        public Dictionary<string, List<BusinessAppAgentIntegrationData>> STT { get; set; } = new Dictionary<string, List<BusinessAppAgentIntegrationData>>();

        [MultiLanguageProperty]
        public Dictionary<string, List<BusinessAppAgentIntegrationData>> TTS { get; set; } = new Dictionary<string, List<BusinessAppAgentIntegrationData>>();

        [MultiLanguageProperty]
        public Dictionary<string, List<BusinessAppAgentIntegrationData>> LLM { get; set; } = new Dictionary<string, List<BusinessAppAgentIntegrationData>>();
    }
}
