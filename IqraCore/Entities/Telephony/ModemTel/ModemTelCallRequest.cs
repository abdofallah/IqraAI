using System.Text.Json.Serialization;

namespace IqraCore.Entities.Telephony.ModemTel
{
    public class ModemTelCallRequest
    {
        [JsonPropertyName("phoneNumberId")]
        public string PhoneNumberId { get; set; }

        [JsonPropertyName("to")]
        public string To { get; set; }
    }
}
