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

        public BusinessPermission Permission { get; set; } = new BusinessPermission();

        // WhiteLabel Assignment
        public string? WhiteLabelAssignedCustomerEmail { get; set; } = null;
    }
}
