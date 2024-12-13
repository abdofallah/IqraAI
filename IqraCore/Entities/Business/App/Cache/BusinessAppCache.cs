namespace IqraCore.Entities.Business
{
    public class BusinessAppCache
    {
        public List<BusinessAppCacheAudioGroup> AudioGroups { get; set; } = new List<BusinessAppCacheAudioGroup>();
        public List<BusinessAppCacheMessageGroup> MessageGroups { get; set; } = new List<BusinessAppCacheMessageGroup>();
    }
}
