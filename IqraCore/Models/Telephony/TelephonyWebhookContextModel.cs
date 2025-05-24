using IqraCore.Entities.Helper.Telephony;

namespace IqraCore.Models.Telephony
{
    public class TelephonyWebhookContextModel
    {
        public TelephonyProviderEnum Provider { get; set; }
        public string CallId { get; set; } = string.Empty;
        public long BusinessId { get; set; } = -1;
        public string PhoneNumberId { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
        public Dictionary<string, string> AdditionalData { get; set; } = new Dictionary<string, string>();
    }
}