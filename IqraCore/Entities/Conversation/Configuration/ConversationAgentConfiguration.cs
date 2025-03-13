namespace IqraCore.Entities.Conversation.Configuration
{
    public class ConversationAgentConfiguration
    {
        public long BusinessId { get; set; }
        public string RouteId { get; set; }
        public string BusinessAgentId { get; set; }
        public string LanguageCode { get; set; }
        public Dictionary<string, string> ContextVariables { get; set; } = new Dictionary<string, string>();
    }
}
