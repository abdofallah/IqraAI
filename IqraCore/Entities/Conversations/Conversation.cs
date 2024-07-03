using IqraCore.Entities.Helper.Session;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Conversation
{
    public class Conversation
    {
        [BsonId]
        public long SessionId { get; set; }
        public SessionTypeENUM ConversationType { get; set; }

        public ConversationGeneral General { get; set; }
        public ConversationConversation Conversation { get; set; }
        public ConversationConfiguration Configuration { get; set; }
        public ConversationAgent Agent { get; set; }
        public ConversationActions Actions { get; set; }
    }
}
