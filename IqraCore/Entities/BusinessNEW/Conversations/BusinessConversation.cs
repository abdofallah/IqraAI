using IqraCore.Entities.Helper.Session;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessConversation
    {
        [BsonId]
        public long SessionId { get; set; }
        public SessionTypeENUM ConversationType { get; set; }

        public BusinessConversationGeneral General { get; set; }
        public BusinessConversationConversation Conversation { get; set; }
        public BusinessConversationConfiguration Configuration { get; set; }
        public BusinessConversationAgent Agent { get; set; }
        public BusinessConversationActions Actions { get; set; }
    }
}
