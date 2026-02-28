using System.Text.Json.Serialization;

namespace IqraCore.Entities.Rerank.Providers.GoogleGemini
{
    public class GeminiRerankBatchResponse
    {
        [JsonPropertyName("embeddings")]
        public List<GeminiRerankEmbedding> Embeddings { get; set; }
    }

    public class GeminiRerankEmbedding
    {
        [JsonPropertyName("values")]
        public float[] Values { get; set; }
    }

    public class GeminiRerankErrorResponse
    {
        [JsonPropertyName("error")]
        public GeminiRerankErrorDetails Error { get; set; }
    }

    public class GeminiRerankErrorDetails
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
