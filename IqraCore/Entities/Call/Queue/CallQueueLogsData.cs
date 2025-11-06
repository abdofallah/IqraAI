using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Call.Queue
{
    public class CallQueueLogsData
    {
        [BsonId]
        public string Id { get; set; }

        public List<CallQueueLogEntry> Logs { get; set; }
    }
}
