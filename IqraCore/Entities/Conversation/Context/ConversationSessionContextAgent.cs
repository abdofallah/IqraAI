namespace IqraCore.Entities.Conversation.Context
{
    public class ConversationSessionContextAgent
    {
        public string SelectedAgentId { get; set; } = string.Empty;
        public string OpeningScriptId { get; set; } = string.Empty;
        public List<string> Timezones { get; set; } = new List<string>();

        public bool TelephonyNumberInContext { get; set; } = false;
        // Inbound
        public bool? CallerNumberInContext { get; set; } = null;
        // Outbound
        public bool? RecipientNumberInContext { get; set; } = null;
    }
}
