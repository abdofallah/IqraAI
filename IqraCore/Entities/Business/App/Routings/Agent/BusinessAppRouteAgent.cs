using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppRouteAgent
    {
        public string SelectedAgentId { get; set; } = string.Empty;
        public string OpeningScriptId { get; set; } = string.Empty;

        // TODO better organize interruption data
        public AgentConversationTypeENUM ConversationType { get; set; } = AgentConversationTypeENUM.TurnByTurn;
        // for AgentConversationTypeENUM.InterruptibleViaVAD
        public int? InterruptibleConversationAudioActivityDurationMS { get; set; } = null; 
        // for AgentConversationTypeENUM.InterruptibleViaAI
        public bool? UseCurrentAgentLLMForInterrupting { get; set; } = null;
        public BusinessAppAgentIntegrationData? LLMIntegrationToUseForCheckingInterruption { get; set; } = null;

        public List<string> Timezones { get; set; } = new List<string>();
        public bool CallerNumberInContext { get; set; } = true;
        public bool RouteNumberInContext { get; set; } = true;
    }
}
