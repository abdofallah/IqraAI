using IqraCore.Entities.Interfaces;

namespace IqraCore.Entities.Business
{
    public class BusinessAppCacheEmbedding
    {
        public string Id { get; set; }
        public string Query { get; set; } = string.Empty;
        public int UnusedExpiryHours { get; set; } = 24;

        public List<BusinessAppCacheEmbeddingCacheLink> GeneratedCacheLinks { get; set; } = new List<BusinessAppCacheEmbeddingCacheLink>();
    }

    public class BusinessAppCacheEmbeddingCacheLink
    {
        public InterfaceEmbeddingProviderEnum Provider { get; set; }
        public int ConfigVersion { get; set; }
        public string CacheKey { get; set; }
    }
}
