using IqraCore.Entities.User.Usage.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Usage
{
    public class OverallUserUsageStatsByTypeResult
    {
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TotalCost { get; set; }
        public Dictionary<string, FeatureUsageBreakdown> UsageByFeature { get; set; } = new Dictionary<string, FeatureUsageBreakdown>();
    }

    public class FeatureUsageBreakdown
    {
        public List<UsageTypeBreakdown> Breakdown { get; set; } = new List<UsageTypeBreakdown>();
    }

    public class UsageTypeBreakdown
    {
        [BsonRepresentation(BsonType.String)]
        public UserUsageConsumedTypeEnum Type { get; set; }
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Quantity { get; set; }
    }
}
