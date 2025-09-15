using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentInterruptionTurnEnd
    {
        public AgentInterruptionTurnEndTypeENUM Type { get; set; } = AgentInterruptionTurnEndTypeENUM.VAD;
        
        // Via VAD
        public int? VadSpeechDurationMS { get; set; } = 150;
        public int? VadSilenceDurationMS { get; set; } = 300;

        // Via AI
        public bool? UseAgentLLM { get; set; } = null;
        public BusinessAppAgentIntegrationData? LLMIntegration { get; set; } = null;

        // Via ML
        public int? MLTurnEndVADMinimumSpeechDurationMS { get; set; } = 150;
        public int? MLTurnEndVADMinimumSilenceDurationMS { get; set; } = 300;
        public int? MlTurnEndFallbackMs { get; set; } = 400;
    }
}