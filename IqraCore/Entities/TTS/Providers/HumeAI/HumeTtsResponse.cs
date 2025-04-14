using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.HumeAI
{
    public class HumeTtsResponse
    {
        [JsonPropertyName("generations")]
        public List<HumeGenerationResponse>? Generations { get; set; }

        [JsonPropertyName("request_id")]
        public string? RequestId { get; set; }

        // Hume API might also include error details directly
        [JsonPropertyName("code")]
        public string? ErrorCode { get; set; }
        [JsonPropertyName("message")]
        public string? ErrorMessage { get; set; }
    }
}
