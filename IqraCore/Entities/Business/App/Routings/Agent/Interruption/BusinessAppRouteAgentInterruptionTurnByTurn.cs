using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppRouteAgentInterruptionTurnByTurn : BusinessAppRouteAgentInterruption
    {
        public override AgentInterruptionTypeENUM Type { get; internal set; } = AgentInterruptionTypeENUM.TurnByTurn;

        public bool UseInterruptedResponseInNextTurn { get; set; } = false;
    }
}
