using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Conversation.Turn
{
    public class ConversationTurn
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public int Sequence { get; set; }

        public ConversationTurnUserInput User { get; set; } = new ConversationTurnUserInput();
        public ConversationTurnAgentResponse Response { get; set; } = new ConversationTurnAgentResponse();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public ConversationTurnTurnStatus Status { get; set; } = ConversationTurnTurnStatus.UserInputStarted;
    }

    public enum ConversationTurnTurnStatus
    {
        UserInputStarted,
        UserInputEnded,
        AgentProcessing,
        AgentRespondingSpeech,
        AgentExecutingTool,
        Completed,
        Interrupted,
        Error
    }

    public enum ConversationTurnAgentResponseType
    {
        NotSet,
        Speech,
        SystemTool,
        CustomTool,
        Error
    }

    public enum ConversationTurnAgentToolType
    {
        System,
        Custom
    }
}