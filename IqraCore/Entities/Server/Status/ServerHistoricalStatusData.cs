using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Server
{
    public class ServerHistoricalStatusData : ServerStatusData
    {
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime DateTime { get; set; }
    }
}
