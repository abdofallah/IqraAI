namespace IqraCore.Entities.Conversation.Context
{
    public class ConversationSessionContextAgent
    {
        public string SelectedAgentId { get; set; } = string.Empty;
        public string OpeningScriptId { get; set; } = string.Empty;
        public List<string> Timezones { get; set; } = new List<string>();

        // Telephony
        public bool? TelephonyNumberInContext { get; set; } = null;
        // Inbound
        public bool? CallerNumberInContext { get; set; } = null;
        // Outbound
        public bool? RecipientNumberInContext { get; set; } = null;
    }
}
