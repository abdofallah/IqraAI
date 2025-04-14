using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Cartesia
{
    public class CartesiaVoiceRequest
    {
        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "id";

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }
}
