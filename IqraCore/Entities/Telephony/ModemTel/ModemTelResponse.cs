using System.Text.Json.Serialization;

namespace IqraCore.Entities.Telephony.ModemTel
{
    public class ModemTelResponse<T>
    {
        [JsonPropertyName("data")]
        public T Data { get; set; }
    }
}
