namespace IqraCore.Entities.Telephony.Call
{
    public class CallSessionMetrics
    {
        public int TotalUserUtterances { get; set; } = 0;
        public int TotalAgentResponses { get; set; } = 0;

        public double AverageResponseTimeMs { get; set; } = 0;
        public double AverageSTTLatencyMs { get; set; } = 0;
        public double AverageLLMLatencyMs { get; set; } = 0;
        public double AverageTTSLatencyMs { get; set; } = 0;

        public Dictionary<string, int> NodesVisited { get; set; } = new Dictionary<string, int>();
        public List<string> ToolsExecuted { get; set; } = new List<string>();
    }
}
