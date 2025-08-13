using System.Text.Json.Serialization;

namespace IqraCore.Entities.Embedding.Providers.GoogleGemini
{
    public class GeminiEmbeddingRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("content")]
        public EmbeddingContent Content { get; set; }

        [JsonPropertyName("task_type")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TaskType { get; set; }

        [JsonPropertyName("output_dimensionality")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? OutputDimensionality { get; set; }
    }

    public class EmbeddingContent
    {
        [JsonPropertyName("parts")]
        public List<ContentPart> Parts { get; set; }
    }

    public class ContentPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }
}
