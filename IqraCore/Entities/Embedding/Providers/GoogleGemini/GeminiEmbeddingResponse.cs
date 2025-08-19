using System.Text.Json.Serialization;

namespace IqraCore.Entities.Embedding.Providers.GoogleGemini
{
    public class GeminiEmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public EmbeddingObject Embedding { get; set; }
    }

    public class GeminiEmbeddingsListResponse
    {
        [JsonPropertyName("embeddings")]
        public List<EmbeddingObject> Embeddings { get; set; }
    }

    public class EmbeddingObject
    {
        [JsonPropertyName("values")]
        public float[] Values { get; set; }
    }
}
