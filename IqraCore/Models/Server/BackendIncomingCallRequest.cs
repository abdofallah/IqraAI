using IqraCore.Entities.Helper.Telephony;

namespace IqraCore.Models.Server
{
    public class BackendIncomingCallRequest
    {
        public TelephonyProviderEnum Provider { get; set; }
        public string CallId { get; set; } = string.Empty;
        public string QueueId { get; set; } = string.Empty;
        public long BusinessId { get; set; }
        public string PhoneNumberId { get; set; } = string.Empty;
        public string RouteId { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public Dictionary<string, string> AdditionalData { get; set; } = new Dictionary<string, string>();
    }
}
