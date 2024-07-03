using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessData
    {
        [BsonId]
        public string Id { get; set; }

        public string Name { get; set; }
        public string Region { get; set; }
        public string LogoURL { get; set; }
        public string DefaultLanguage { get; set; }
        public List<string> Languages { get; set; }
        public BusinessAnalytics Analytics { get; set; }
        public List<BusinessUser> Users { get; set; }
    }
}
