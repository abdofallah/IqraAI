using IqraCore.Entities.Business;

namespace IqraCore.Models.User.Business.App.Cache
{
    public class BusinessAppCacheModel
    {
        public BusinessAppCacheModel(BusinessAppCache data)
        {
            AudioGroups = data.AudioGroups.Select(x => new BusinessAppCacheAudioGroupModel(x)).ToList();
            MessageGroups = data.MessageGroups.Select(x => new BusinessAppCacheMessageGroupModel(x)).ToList();
            EmbeddingGroups = data.EmbeddingGroups.Select(x => new BusinessAppCacheEmbeddingGroupModel(x)).ToList();
        }

        public List<BusinessAppCacheAudioGroupModel> AudioGroups { get; set; }
        public List<BusinessAppCacheMessageGroupModel> MessageGroups { get; set; }
        public List<BusinessAppCacheEmbeddingGroupModel> EmbeddingGroups { get; set; }
    }
}
