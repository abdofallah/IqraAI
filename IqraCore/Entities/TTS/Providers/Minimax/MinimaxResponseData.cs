using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Minimax
{
    public class MinimaxResponseData
    {
        [JsonPropertyName("audio")]
        public string? Audio { get; set; } // Hex encoded audio string

        [JsonPropertyName("status")]
        public int Status { get; set; } // 1 = in progress, 2 = completed (for streaming)
    }
}
