using IqraCore.Entities.Business.Integration;

namespace IqraCore.Entities.Business
{
    public class BusinessAppIntegration
    {
        public List<BusinessAppIntegrationSTT> STT { get; set; } = new List<BusinessAppIntegrationSTT>();
        public List<BusinessAppIntegrationTTS> TTS { get; set; } = new List<BusinessAppIntegrationTTS>();
        public List<BusinessAppIntegrationLLM> LLM { get; set; } = new List<BusinessAppIntegrationLLM>();
    }
}
