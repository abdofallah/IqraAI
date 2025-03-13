namespace IqraCore.Entities.Conversation
{
    public class ConversationMetrics
    {
        public double DurationSeconds { get; set; }
        public int ClientMessageCount { get; set; }
        public int AgentMessageCount { get; set; }
        public double AverageAgentResponseTimeMs { get; set; }
        public int ClientWordCount { get; set; }
        public int AgentWordCount { get; set; }
        public int ClientInterruptionCount { get; set; }
        public int AgentInterruptionCount { get; set; }
        public int SilenceCount { get; set; }
        public double TotalSilenceDurationSeconds { get; set; }
        public double SttAverageLatencyMs { get; set; }
        public double LlmAverageLatencyMs { get; set; }
        public double TtsAverageLatencyMs { get; set; }
        public Dictionary<string, double> AdditionalMetrics { get; set; } = new Dictionary<string, double>();
    }
}
