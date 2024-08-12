using IqraCore.Attributes;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentCache
    {
        [MultiLanguageProperty]
        public Dictionary<string, List<BusinessAppAgentCacheMessage>> Messages { get; set; } = new Dictionary<string, List<BusinessAppAgentCacheMessage>>();

        [MultiLanguageProperty]
        public Dictionary<string, List<BusinessAppAgentCacheAudio>> Audios { get; set; } = new Dictionary<string, List<BusinessAppAgentCacheAudio>>();
        public BusinessAppAgentCacheSettings Settings { get; set; } = new BusinessAppAgentCacheSettings();
    }
}
