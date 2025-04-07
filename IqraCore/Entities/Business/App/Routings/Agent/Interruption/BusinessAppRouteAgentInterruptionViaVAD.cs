using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppRouteAgentInterruptionViaVAD : BusinessAppRouteAgentInterruption
    {
        public override AgentInterruptionTypeENUM Type { get; internal set; } = AgentInterruptionTypeENUM.InterruptibleViaVAD;

        public int InterruptibleConversationAudioActivityDurationMS { get; set; } = 300;
    }
}
