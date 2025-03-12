using System.Text.Json.Serialization;

namespace IqraCore.Models.Server
{
    public class OutboundCallRequestModel
    {
        [JsonPropertyName("businessId")]
        public long BusinessId { get; set; }

        [JsonPropertyName("phoneNumberId")]
        public string PhoneNumberId { get; set; } = string.Empty;

        [JsonPropertyName("toNumber")]
        public string ToNumber { get; set; } = string.Empty;

        [JsonPropertyName("callConfigurationId")]
        public string CallConfigurationId { get; set; } = string.Empty;

        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
