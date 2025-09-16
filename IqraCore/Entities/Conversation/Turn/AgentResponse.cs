namespace IqraCore.Entities.Conversation.Turn
{
    public class AgentResponse
    {
        public string AgentId { get; set; }
        public AgentResponseType Type { get; set; } = AgentResponseType.NotSet;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public AgentResponseStatus Status { get; set; } = AgentResponseStatus.Pending;

        // For Speech
        public List<SpeechSegmentData> SpokenSegments { get; set; } = new List<SpeechSegmentData>();

        // For Tools
        public ToolExecutionData? ToolExecution { get; set; }
    }
}