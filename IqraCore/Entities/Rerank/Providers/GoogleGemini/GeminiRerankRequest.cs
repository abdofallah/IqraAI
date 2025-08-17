using System.Text.Json.Serialization;

namespace IqraCore.Entities.Rerank.Providers.GoogleGemini
{
    public class GeminiRerankRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("query")]
        public string Query { get; set; }

        [JsonPropertyName("contents")]
        public List<RerankContent> Contents { get; set; }

        [JsonPropertyName("top_n")]
        public int TopN { get; set; }
    }

    public class RerankContent
    {
        [JsonPropertyName("parts")]
        public List<RerankContentPart> Parts { get; set; }
    }

    public class RerankContentPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }
}
