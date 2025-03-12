namespace IqraCore.Models.Telephony
{
    public class TwilioStatusCallbackDataModel
    {
        public string CallSid { get; set; } = string.Empty;
        public string CallStatus { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public string AccountSid { get; set; } = string.Empty;
    }
}
