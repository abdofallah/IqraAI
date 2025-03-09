using System.Text.Json.Serialization;

namespace IqraCore.Entities.Numbers.Providers.ModemTel
{
    public class ModemTelMessageRequest
    {
        [JsonPropertyName("phoneNumberId")]
        public string PhoneNumberId { get; set; }

        [JsonPropertyName("to")]
        public string To { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; }
    }
}
