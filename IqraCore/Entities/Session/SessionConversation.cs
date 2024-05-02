using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities
{
    public class SessionConversation
    {
        [BsonId]
        private ObjectId Id {  get; set; }
        public string SessionId { get; set; }
        public List<ConversationData> ConversationList { get; set; }
    }

    public class ConversationData
    {
        public DateTime Timestamp { get; set; }
        public string UserInput { get; set; }
        public string AIResponse { get; set; }
    }
}