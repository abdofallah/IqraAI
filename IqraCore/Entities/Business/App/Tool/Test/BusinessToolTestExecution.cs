using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Business
{
    public class BusinessToolTestExecution
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        public long BusinessId { get; set; }
        public string ToolId { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;
        public string ExecutedByEmail { get; set; } = string.Empty;
        public string ExecutedByIp { get; set; } = string.Empty;
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object?> InputParameters { get; set; } = new();
        public string LanguageCode { get; set; } = "en";
        public BusinessToolTestResult Result { get; set; } = new();
    }
}
