using System.Text.Json.Serialization;

namespace IqraCore.Entities.Telephony.ModemTel
{
    public class ModemTelCallRequest
    {
        [JsonPropertyName("phoneNumberId")]
        public string PhoneNumberId { get; set; }

        [JsonPropertyName("to")]
        public string To { get; set; }

        [JsonPropertyName("streamUrl")]
        public string StreamUrl { get; set; }

        [JsonPropertyName("streamToken")]
        public string StreamToken { get; set; }

        [JsonPropertyName("statusCallback")]
        public string StatusCallback { get; set; }
    }
}
