namespace IqraCore.Entities.Conversation.Turn
{
    public class ConversationTurnAgentResponse
    {
        public string AgentId { get; set; }
        public ConversationTurnAgentResponseType Type { get; set; } = ConversationTurnAgentResponseType.NotSet;

        // If LLM Response
        public DateTime? LLMProcessStartedAt { get; set; } = null;
        public DateTime? LLMStreamingStartedAt { get; set; } = null;
        public DateTime? LLMStreamingCompletedAt { get; set; } = null;
        public int? LLMProcessLatencyFirstTokenMS { get; set; } = null;

        // For Speech
        public List<ConversationTurnSpeechSegmentData> SpokenSegments { get; set; } = new List<ConversationTurnSpeechSegmentData>();
        public DateTime? SpeechCompletedAt { get; set; } = null;

        // For Tools
        public ConversationTurnToolExecutionData? ToolExecution { get; set; }

        // For Knowledge Base
        public ConversationTurnKnowledgeBaseRetrievalData? KnowledgeBaseRetrievalData { get; set; }
    }
}