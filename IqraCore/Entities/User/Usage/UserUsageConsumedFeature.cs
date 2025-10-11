using IqraCore.Entities.User.Usage.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.User.Usage
{
    public class UserUsageConsumedFeature
    {
        public string FeatureKey { get; set; } = string.Empty;
        public UserUsageConsumedTypeEnum Type { get; set; } = UserUsageConsumedTypeEnum.Unknown;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Quantity { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal AppliedUnitUsage { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TotalUsage { get; set; } = 0;
    }
}
