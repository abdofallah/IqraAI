namespace IqraCore.Models.Embedding.Cache
{
    public enum CacheHitStatus
    {
        MISS,
        HIT,
    }

    public record EmbeddingCacheGetResult(
        CacheHitStatus Status,
        List<float> Vector
    )
    {
        public bool IsHit => Status == CacheHitStatus.HIT && Vector != null && Vector.Count > 0;

        // Static factory for convenience
        public static EmbeddingCacheGetResult Miss() => new(CacheHitStatus.MISS, new List<float>());
    }
}
