using System.Text.Json.Serialization;

namespace IqraCore.Entities.Numbers.Providers.ModemTel
{
    public class ModemTelDtmfSendModel
    {
        [JsonPropertyName("digits")]
        public string Digits { get; set; }
    }
}
