namespace IqraCore.Models.TTS.Cache
{
    public enum CacheHitStatus
    {
        MISS,
        HIT,
    }

    public record CacheGetResult(
        CacheHitStatus Status,
        ReadOnlyMemory<byte> AudioData,
        TimeSpan Duration
    )
    {
        public bool IsHit => Status == CacheHitStatus.HIT && !AudioData.IsEmpty;
        public static CacheGetResult Miss() => new(CacheHitStatus.MISS, ReadOnlyMemory<byte>.Empty, TimeSpan.Zero);
    }

    public record RedisCachePointer(string Path, TimeSpan Duration, string OriginRegion);
}
