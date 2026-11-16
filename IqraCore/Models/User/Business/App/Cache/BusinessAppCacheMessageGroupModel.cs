using IqraCore.Entities.Business;

namespace IqraCore.Models.User.Business.App.Cache
{
    public class BusinessAppCacheMessageGroupModel
    {
        public BusinessAppCacheMessageGroupModel(BusinessAppCacheMessageGroup data)
        {
            Id = data.Id;
            Name = data.Name;
            Messages = data.Messages.ToDictionary(x => x.Key, x => x.Value.Select(m => new BusinessAppCacheMessageModel(m)).ToList());
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public Dictionary<string, List<BusinessAppCacheMessageModel>> Messages { get; set; }
    }
}
