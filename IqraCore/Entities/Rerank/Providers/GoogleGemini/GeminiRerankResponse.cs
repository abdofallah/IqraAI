using System.Text.Json.Serialization;

namespace IqraCore.Entities.Rerank.Providers.GoogleGemini
{
    public class GeminiRerankResponse
    {
        [JsonPropertyName("ranked_documents")]
        public List<RankedDocument> RankedDocuments { get; set; }
    }

    public class RankedDocument
    {
        // Google does not return the original index, so we will manage this ourselves.
        // It does return the content, which we can match.
        [JsonPropertyName("content")]
        public RerankContent Content { get; set; }

        [JsonPropertyName("score")]
        public double Score { get; set; }
    }
}
