using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessData
    {
        [BsonId]
        public long Id { get; set; }
        public string UserEmail { get; set; }

        public string Name { get; set; }
        public string Region { get; set; }
        public string LogoURL { get; set; }
        public string DefaultLanguage { get; set; }
        public List<string> Languages { get; set; } = new List<string>();
        public BusinessAnalytics Analytics { get; set; } = new BusinessAnalytics();
        public List<BusinessUser> Users { get; set; } = new List<BusinessUser>();

        public List<long> NumberIds { get; set; } = new List<long>();
    }
}
