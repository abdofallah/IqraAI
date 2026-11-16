using IqraCore.Entities.Business;

namespace IqraCore.Models.User.Business.App.Cache
{
    public class BusinessAppCacheAudioGroupModel
    {
        public BusinessAppCacheAudioGroupModel(BusinessAppCacheAudioGroup data)
        {
            Id = data.Id;
            Name = data.Name;
            Audios = data.Audios.ToDictionary(x => x.Key, x => x.Value.Select(a => new BusinessAppCacheAudioModel(a)).ToList());
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public Dictionary<string, List<BusinessAppCacheAudioModel>> Audios { get; set; }
    }
}
