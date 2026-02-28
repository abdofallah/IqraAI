using System.Text.Json;
using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Inworld
{
    public class InworldTtsResponse
    {
        [JsonPropertyName("audioContent")]
        public string? AudioContent { get; set; }

        [JsonPropertyName("error")]
        public InworldTtsError? Error { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    public class InworldTtsError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
