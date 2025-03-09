using System.Text.Json.Serialization;

namespace IqraCore.Entities.Numbers.Providers.ModemTel
{
    public class ModemTelResponse<T>
    {
        [JsonPropertyName("data")]
        public T Data { get; set; }
    }
}
