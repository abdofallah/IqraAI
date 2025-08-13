using System.Text.Json.Serialization;

namespace IqraCore.Entities.Embedding.Providers.GoogleGemini
{
    public class GeminiErrorResponse
    {
        [JsonPropertyName("error")]
        public ErrorDetails Error { get; set; }
    }

    public class ErrorDetails
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }
}
