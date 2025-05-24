using System.Text.Json.Serialization;

namespace IqraCore.Entities.Telephony.ModemTel
{
    public class ModemTelWebhookStatusData
    {
        public string CallId { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string CallStatus { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public int? Duration { get; set; } // Call duration in seconds

        public string? ErrorCode { get; set; } // If CallStatus is failed
        public string? ErrorInfo { get; set; } // More details on the error
    }
}
