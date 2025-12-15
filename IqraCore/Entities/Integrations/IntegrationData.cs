using IqraCore.Entities.S3Storage;
using MongoDB.Bson;

namespace IqraCore.Entities.Integrations
{
    public class IntegrationData
    {
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? DisabledAt { get; set; } = null;
        public S3StorageFileLink? LogoS3StorageLink { get; set; } = null;
        public List<string> Type { get; set; } = new List<string>();
        public List<IntegrationFieldData> Fields { get; set; } = new List<IntegrationFieldData>();
        public IntegrationHelpData Help { get; set; } = new IntegrationHelpData();
    }
}
