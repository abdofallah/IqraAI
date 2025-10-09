using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Conversation.Logs
{
    public class ConversationStateLogsData
    {
        [BsonId]
        public string Id { get; set; }

        public List<ConversationStateLogEntry> Logs { get; set; } = new List<ConversationStateLogEntry>();
    }
}
