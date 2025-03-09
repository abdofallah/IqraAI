using System.Text.Json.Serialization;

namespace IqraCore.Entities.Numbers.Providers.ModemTel
{
    public class ModemTelPhoneNumberDetails : ModemTelPhoneNumber
    {
        [JsonPropertyName("webhookUrl")]
        public string WebhookUrl { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("isInUse")]
        public bool IsInUse { get; set; }
    }
}
