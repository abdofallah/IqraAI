using IqraCore.Entities.Business;

namespace IqraCore.Models.User.Business.App.Cache
{
    public class BusinessAppCacheEmbeddingModel
    {
        public BusinessAppCacheEmbeddingModel(BusinessAppCacheEmbedding data)
        {
            Id = data.Id;
            Query = data.Query;
            UnusedExpiryHours = data.UnusedExpiryHours;
        }

        public string Id { get; set; }
        public string Query { get; set; }
        public int UnusedExpiryHours { get; set; }
    }
}
