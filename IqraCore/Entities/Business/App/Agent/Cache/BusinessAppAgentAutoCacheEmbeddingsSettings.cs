namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentAutoCacheEmbeddingsSettings
    {
        public bool AutoCacheEmbeddingResponses { get; set; } = false;
        public int? AutoCacheEmbeddingResponsesDefaultExpiryHours { get; set; } = 24;
        public string? AutoCacheEmbeddingResponseCacheGroupId { get; set; } = null;
    }
}