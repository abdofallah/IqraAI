using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace IqraCore.Entities.App.Configuration
{
    public class ApiKey
    {

        [BsonElement("key")]
        public string Key { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}
