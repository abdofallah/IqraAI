using IqraCore.Entities.Conversation.Turn;

namespace IqraCore.Models.Business.Conversations
{
    /// <summary>
    /// 1:1 ViewModel hierarchy mirroring ConversationTurn entity structure.
    /// Provides structured turn data for frontend conversation viewer.
    /// </summary>
    public class ConversationTurnViewModel
    {
        public string Id { get; set; }
        public int Sequence { get; set; }
        public ConversationTurnType Type { get; set; }
        public ConversationTurnStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public ConversationTurnUserInputViewModel? UserInput { get; set; }
        public ConversationTurnSystemInputViewModel? SystemInput { get; set; }
        public ConversationTurnToolResultInputViewModel? ToolResultInput { get; set; }

        public ConversationTurnAgentResponseViewModel Response { get; set; }
    }

    public class ConversationTurnUserInputViewModel
    {
        public string? TranscribedText { get; set; }
        public DateTime StartedSpeakingAt { get; set; }
        public DateTime? FinishedSpeakingAt { get; set; }
    }

    public class ConversationTurnSystemInputViewModel
    {
        public string Type { get; set; }
    }

    public class ConversationTurnToolResultInputViewModel
    {
        public string ResultOfTurnId { get; set; }
    }

    public class ConversationTurnAgentResponseViewModel
    {
        public string AgentId { get; set; }
        public ConversationTurnAgentResponseType Type { get; set; }

        // LLM Processing Timestamps
        public DateTime? LLMProcessStartedAt { get; set; }
        public DateTime? LLMStreamingStartedAt { get; set; }
        public DateTime? LLMStreamingCompletedAt { get; set; }
        public int? LLMProcessLatencyFirstTokenMS { get; set; }

        // Speech
        public List<ConversationTurnSpeechSegmentViewModel> SpokenSegments { get; set; } = new();
        public DateTime? SpeechCompletedAt { get; set; }

        // Tool Execution
        public ConversationTurnToolExecutionViewModel? ToolExecution { get; set; }

        // Knowledge Base
        public ConversationTurnKBRetrievalViewModel? KnowledgeBaseRetrievalData { get; set; }
    }

    public class ConversationTurnSpeechSegmentViewModel
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public DateTime StartedPlayingAt { get; set; }
        public DateTime? FinishedPlayingAt { get; set; }
        public bool WasInterrupted { get; set; }
        public TimeSpan Duration { get; set; }
        public bool IsCacheHit { get; set; }
        public int RetrievalLatencyMS { get; set; }
    }

    public class ConversationTurnToolExecutionViewModel
    {
        public ConversationTurnAgentToolType ToolType { get; set; }
        public string? NodeId { get; set; }
        public string? ReasonForExecution { get; set; }
        public string ToolName { get; set; }
        public string RawLLMInput { get; set; }
        public string? Result { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool WasSuccessful { get; set; }
    }

    public class ConversationTurnKBRetrievalViewModel
    {
        public bool WasSuccessfull { get; set; }
        public string? ResultMessage { get; set; }
        public List<Dictionary<string, object>>? RetrievedResultsMetaData { get; set; }
        public int RetrievalLatencyMS { get; set; }
    }
}
