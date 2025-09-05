using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentInterruptionTurnEnd
    {
        public AgentInterruptionTurnEndTypeENUM Type { get; set; } = AgentInterruptionTurnEndTypeENUM.VAD;
        
        // Via VAD
        public int? VadSilenceDurationMS { get; set; } = null;

        // Via AI
        public bool? UseAgentLLM { get; set; } = null;
        public BusinessAppAgentIntegrationData? LLMIntegration { get; set; } = null;
    }
}