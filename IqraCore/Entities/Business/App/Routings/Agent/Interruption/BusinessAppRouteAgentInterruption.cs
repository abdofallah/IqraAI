using IqraCore.Entities.Helper.Agent;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Business
{
    [BsonKnownTypes(
        typeof(BusinessAppRouteAgentInterruptionTurnByTurn),
        typeof(BusinessAppRouteAgentInterruptionViaAI),
        typeof(BusinessAppRouteAgentInterruptionViaResponse),
        typeof(BusinessAppRouteAgentInterruptionViaVAD)
    )]
    public class BusinessAppRouteAgentInterruption
    {
        public virtual AgentInterruptionTypeENUM Type { get; internal set; } = AgentInterruptionTypeENUM.TurnByTurn;
    }
}
