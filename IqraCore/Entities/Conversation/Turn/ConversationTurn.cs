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

        public UserInput User { get; set; } = new UserInput();
        public AgentResponse Response { get; set; } = new AgentResponse();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public TurnStatus Status { get; set; } = TurnStatus.UserInputStarted;
    }

    public enum TurnStatus
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

    public enum AgentResponseType
    {
        NotSet,
        Speech,
        SystemTool,
        CustomTool,
        Error
    }

    public enum AgentResponseStatus
    {
        Pending,
        Processing,
        Completed,
        Cancelled, // Interrupted before it could even start
        Interrupted, // Interrupted mid-stream
        Error
    }

    public enum AgentToolType
    {
        System,
        Custom
    }
}