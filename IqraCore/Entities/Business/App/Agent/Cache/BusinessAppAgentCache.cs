namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentCache
    {
        public List<BusinessAppAgentCacheMessage> Messages { get; set; } = new List<BusinessAppAgentCacheMessage>();
        public List<BusinessAppAgentCacheAudio> Audios { get; set; } = new List<BusinessAppAgentCacheAudio>();
        public BusinessAppAgentCacheSettings Settings { get; set; } = new BusinessAppAgentCacheSettings();
    }
}
