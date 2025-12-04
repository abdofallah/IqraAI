using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Conversation.Turn
{
    public class ConversationTurn
    {
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public int Sequence { get; set; }

        public ConversationTurnType Type { get; set; }

        public ConversationTurnUserInput? UserInput { get; set; } = null;
        public ConversationTurnSystemInput? SystemInput { get; set; } = null;
        public ConversationTurnToolResultInput? ToolResultInput { get; set; } = null;

        public ConversationTurnAgentResponse Response { get; set; } = new ConversationTurnAgentResponse();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public ConversationTurnStatus Status { get; set; } = ConversationTurnStatus.UserInputStarted;
    }

    public enum ConversationTurnType
    {
        NotSet,
        System,
        ToolResult,
        User
    }

    public enum ConversationTurnStatus
    {
        UserInputStarted,
        UserInputEnded,
        AgentProcessing,
        AgentRespondingSpeech,
        AgentExecutingTool,
        Completed,
        Interrupted,
        Error,
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