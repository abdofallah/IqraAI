using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace IqraCore.Entities.Usage
{
    public class AggregatedUsageStatsResult
    {
        [BsonElement("_id")]
        public BsonDocument Id { get; set; }

        public string Period => Id["period"].AsString;
        public long BusinessId => Id["businessId"].AsInt64;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TotalCost { get; set; }

        public Dictionary<string, decimal> UsageByFeature { get; set; } = new Dictionary<string, decimal>();
    }
}
