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

        public string DefaultLanguage { get; set; } = string.Empty;
        public List<string> Languages { get; set; } = new List<string>();

        public Dictionary<string, object> Tutorials { get; set; } = new Dictionary<string, object>();

        [ExcludeInEndpoint("/app/user/businesses")]
        public List<BusinessUser> SubUsers { get; set; } = new List<BusinessUser>();

        public List<long> WhiteLabelDomainIds { get; set; } = new List<long>();

        public BusinessPermission Permission { get; set; } = new BusinessPermission();

        // Plan/Billing/Allocation
        public decimal? AllocatedMonthlyMinuteCap { get; set; } = null;
        public decimal? CurrentMonthlyMinuteUsage { get; set; } = null;

        public int? AllocatedConcurrencySlots { get; set; } = null;
    }
}
