namespace IqraCore.Models.Telephony
{
    public class TwilioWebhookDataModel
    {
        public string CallSid { get; set; } = string.Empty;
        public string AccountSid { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string CallStatus { get; set; } = string.Empty;
        public string ApiVersion { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
        public string ForwardedFrom { get; set; } = string.Empty;
        public string CallerName { get; set; } = string.Empty;
    }
}
