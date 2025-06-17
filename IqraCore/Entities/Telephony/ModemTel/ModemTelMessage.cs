using System.Text.Json.Serialization;

namespace IqraCore.Entities.Telephony.ModemTel
{
    public class ModemTelMessage
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("phoneNumberId")]
        public string PhoneNumberId { get; set; }

        [JsonPropertyName("from")]
        public string From { get; set; }

        [JsonPropertyName("to")]
        public string To { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; }

        [JsonPropertyName("direction")]
        public string Direction { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("createdAt")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("sentAt")]
        public string? SentAt { get; set; }

        [JsonPropertyName("deliveredAt")]
        public string? DeliveredAt { get; set; }
    }
}
