using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Usage
{
    public class AggregatedUsageResult
    {
        [BsonId]
        public string Id { get; set; } // This will be the date/hour string
        public decimal TotalMinutes { get; set; }
    }
}
