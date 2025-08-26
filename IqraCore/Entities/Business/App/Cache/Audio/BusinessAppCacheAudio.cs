using IqraCore.Entities.Interfaces;

namespace IqraCore.Entities.Business
{
    public class BusinessAppCacheAudio
    {
        public string Id { get; set; }
        public string Query { get; set; } = string.Empty;
        public int UnusedExpiryHours { get; set; } = 24;

        public List<BusinessAppCacheAudioCacheLink> GeneratedCacheLinks { get; set; } = new List<BusinessAppCacheAudioCacheLink>();
    }

    public class BusinessAppCacheAudioCacheLink
    {
        public InterfaceTTSProviderEnum Provider { get; set; }
        public int ConfigVersion { get; set; }
        public string CacheKey { get; set; } // This is the Id of the TTSAudioCacheEntry
    }
}
