namespace IqraCore.Entities.Telephony.ModemTel
{
    public class ModemTelWebhookData
    {
        public string Event { get; set; } = string.Empty;
        public string CallId { get; set; } = string.Empty;
        public string PhoneNumberId { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? Timestamp { get; set; }

        // Additional event-specific fields
        public ModemTelMediaSession? MediaSession { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Recording { get; set; }
        public string? Digits { get; set; }
    }
}
