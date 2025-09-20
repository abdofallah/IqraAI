namespace IqraCore.Entities.Conversation.Turn
{
    public class ConversationTurnAgentResponse
    {
        public string AgentId { get; set; }
        public ConversationTurnAgentResponseType Type { get; set; } = ConversationTurnAgentResponseType.NotSet;
        public DateTime? LLMProcessStartedAt { get; set; }
        public DateTime? LLMStreamingStartedAt { get; set; }
        public DateTime? LLMStreamingCompletedAt { get; set; }
        public int LLMProcessLatencyFirstTokenMS { get; set; }

        public DateTime? SpeechCompletedAt { get; set; }

        // For Speech
        public List<ConversationTurnSpeechSegmentData> SpokenSegments { get; set; } = new List<ConversationTurnSpeechSegmentData>();

        // For Tools
        public ConversationTurnToolExecutionData? ToolExecution { get; set; }

        // For Knowledge Base
        public ConversationTurnKnowledgeBaseRetrievalData? KnowledgeBaseRetrievalData { get; set; }
    }
}