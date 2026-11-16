using IqraCore.Entities.Business;

namespace IqraCore.Models.User.Business.App.Cache
{
    public class BusinessAppCacheMessageModel
    {
        public BusinessAppCacheMessageModel(BusinessAppCacheMessage data)
        {
            Id = data.Id;
            Query = data.Query;
            Response = data.Response;
        }

        public string Id { get; set; }
        public string Query { get; set; }
        public string Response { get; set; }
    }
}
