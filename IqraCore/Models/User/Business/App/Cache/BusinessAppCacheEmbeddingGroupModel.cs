using IqraCore.Entities.Business;

namespace IqraCore.Models.User.Business.App.Cache
{
    public class BusinessAppCacheEmbeddingGroupModel
    {
        public BusinessAppCacheEmbeddingGroupModel(BusinessAppCacheEmbeddingGroup data)
        {
            Id = data.Id;
            Name = data.Name;
            Embeddings = data.Embeddings.ToDictionary(x => x.Key, x => x.Value.Select(e => new BusinessAppCacheEmbeddingModel(e)).ToList());
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public Dictionary<string, List<BusinessAppCacheEmbeddingModel>> Embeddings { get; set; }
    }
}
