using IqraCore.Attributes;

namespace IqraCore.Entities.Business
{
    public class BusinessAppCacheAudioGroup
    {
        public string Id { get; set; }
        public string Name { get; set; }

        [MultiLanguageProperty]
        public Dictionary<string, List<BusinessAppCacheAudio>> Audios { get; set; } = new Dictionary<string, List<BusinessAppCacheAudio>>();
    }
}
