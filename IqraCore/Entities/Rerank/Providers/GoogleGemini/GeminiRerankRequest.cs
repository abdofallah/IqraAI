using System.Text.Json.Serialization;

namespace IqraCore.Entities.Rerank.Providers.GoogleGemini
{
    public class GeminiRerankBatchRequest
    {
        [JsonPropertyName("requests")]
        public List<GeminiRerankRequestItem> Requests { get; set; }
    }

    public class GeminiRerankRequestItem
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("content")]
        public GeminiRerankContent Content { get; set; }

        [JsonPropertyName("taskType")]
        public string TaskType { get; set; }
    }

    public class GeminiRerankContent
    {
        [JsonPropertyName("parts")]
        public List<GeminiRerankPart> Parts { get; set; }
    }

    public class GeminiRerankPart
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }
}
