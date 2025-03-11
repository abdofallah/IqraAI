using System.Text.Json.Serialization;

namespace IqraCore.Entities.Telephony.ModemTel
{
    public class ModemTelMediaSession
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("webSocketUrl")]
        public string WebSocketUrl { get; set; }
    }
}
