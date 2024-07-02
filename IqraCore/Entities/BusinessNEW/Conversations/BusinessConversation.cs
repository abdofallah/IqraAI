using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessConversation
    {
        [BsonId]
        public long SessionId { get; set; }
        
        public BusinessConversationGeneral General { get; set; }
        public BusinessConversationConversation Conversation { get; set; }
        public BusinessConversationAgent Agent { get; set; }
    }
}
