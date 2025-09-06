namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentKnowledgeBaseRefinement
    {
        public bool Enabled { get; set; } = false;
        public int? QueryCount { get; set; } = null;
        public bool? UseAgentLLM { get; set; } = null;
        public BusinessAppAgentIntegrationData? LLMIntegration { get; set; } = null;
    }
}