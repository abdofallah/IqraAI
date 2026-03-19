namespace IqraCore.Models.WebSession
{
    public class InitiateWebSessionResultModel
    {
        public string WebSessionId { get; set; } = string.Empty;
        public string ConversationSessionId { get; set; } = string.Empty;
        public string SessionWebSocketURL { get; set; } = string.Empty;
    }
}
