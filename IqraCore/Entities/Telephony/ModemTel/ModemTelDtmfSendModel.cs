using System.Text.Json.Serialization;

namespace IqraCore.Entities.Telephony.ModemTel
{
    public class ModemTelDtmfSendModel
    {
        [JsonPropertyName("digits")]
        public string Digits { get; set; }
    }
}
