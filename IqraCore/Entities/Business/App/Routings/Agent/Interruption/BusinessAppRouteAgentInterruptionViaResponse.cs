using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppRouteAgentInterruptionViaResponse : BusinessAppRouteAgentInterruption
    {
        public override AgentInterruptionTypeENUM Type { get; internal set; } = AgentInterruptionTypeENUM.InterruptibleViaResponse;
    }
}
