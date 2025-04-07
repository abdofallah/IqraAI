using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppRouteAgentInterruptionViaAI : BusinessAppRouteAgentInterruption
    {
        public override AgentInterruptionTypeENUM Type { get; internal set; } = AgentInterruptionTypeENUM.InterruptibleViaAI;

        public bool UseCurrentAgentLLMForInterrupting { get; set; } = false;
        public BusinessAppAgentIntegrationData? LLMIntegrationToUseForCheckingInterruption { get; set; } = null;
    }
}
