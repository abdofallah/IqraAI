using System.Text.Json.Serialization;

namespace IqraCore.Models.Server
{
    public class OutboundCallResultModel
    {
        [JsonPropertyName("queueId")]
        public string QueueId { get; set; } = string.Empty;

        [JsonPropertyName("callId")]
        public string CallId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}
