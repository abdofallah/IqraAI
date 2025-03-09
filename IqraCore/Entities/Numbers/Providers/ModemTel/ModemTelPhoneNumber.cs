using System.Text.Json.Serialization;

namespace IqraCore.Entities.Numbers.Providers.ModemTel
{
    public class ModemTelPhoneNumber
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("countryCode")]
        public string CountryCode { get; set; }

        [JsonPropertyName("number")]
        public string Number { get; set; }

        [JsonPropertyName("friendlyName")]
        public string FriendlyName { get; set; }

        [JsonPropertyName("canMakeCalls")]
        public bool CanMakeCalls { get; set; }

        [JsonPropertyName("canSendSms")]
        public bool CanSendSms { get; set; }
    }
}
