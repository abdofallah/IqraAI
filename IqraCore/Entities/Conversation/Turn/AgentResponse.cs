namespace IqraCore.Entities.Conversation.Turn
{
    public class AgentResponse
    {
        public string AgentId { get; set; }
        public AgentResponseType Type { get; set; } = AgentResponseType.NotSet;
        public DateTime? LLMProcessStartedAt { get; set; }
        public DateTime? LLMProcessCompletedAt { get; set; }

        public DateTime? SpeechCompletedAt { get; set; }

        // For Speech
        public List<SpeechSegmentData> SpokenSegments { get; set; } = new List<SpeechSegmentData>();

        // For Tools
        public ToolExecutionData? ToolExecution { get; set; }
    }
}