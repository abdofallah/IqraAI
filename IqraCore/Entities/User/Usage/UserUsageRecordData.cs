using IqraCore.Entities.User.Usage.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.User.Usage
{
    public class UserUsageRecordData
    {
        [BsonId]
        public string Id { get; set; } = string.Empty;
        public string BusinessMasterUserEmail { get; set; } = string.Empty;
        public long BusinessId { get; set; }
        public string PlanId { get; set; } = string.Empty;

        public List<UserUsageConsumedFeature> ConsumedFeatures { get; set; } = new List<UserUsageConsumedFeature>();

        public string Description { get; set; } = string.Empty;

        public UserUsageSourceTypeEnum SourceType { get; set; } = UserUsageSourceTypeEnum.Unknown;
        public string SourceId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
