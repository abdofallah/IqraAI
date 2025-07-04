using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace IqraCore.Entities.Usage
{
    public class AggregatedUsageStatsResult
    {
        [BsonElement("_id")]
        public string Id { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TotalMinutes { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TotalCost { get; set; }

        public int TotalCalls { get; set; }
    }
}
