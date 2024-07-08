using IqraCore.Attributes;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Business
{
    public class BusinessData
    {
        [BsonId]
        public long Id { get; set; } = -1;
        public string MasterUserEmail { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string LogoURL { get; set; } = string.Empty;

        public string Region { get; set; } = string.Empty;

        public string DefaultLanguage { get; set; } = string.Empty;
        public List<string> Languages { get; set; } = new List<string>();

        public List<long> NumberIds { get; set; } = new List<long>();

        [ExcludeInEndpoint("/app/user/businesses")]
        public List<BusinessUser> SubUsers { get; set; } = new List<BusinessUser>();

        [ExcludeInAllEndpoints]
        [IncludeInEndpoint("/app/admin/user/businesses")]
        public BusinessAnalytics Analytics { get; set; } = new BusinessAnalytics();
    }
}
