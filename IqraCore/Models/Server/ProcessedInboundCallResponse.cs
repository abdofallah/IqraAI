namespace IqraCore.Models.Server
{
    public class ProcessedInboundCallResponse
    {
        public string SessionId { get; set; }
        public string WebhookUrl { get; set; }
        public string WebhookToken { get; set; }
    }
}
