using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentKnowledgeBaseSearchStrategy
    {
        public AgentKnowledgeBaseSearchStartegyTypeENUM Type { get; set; } = AgentKnowledgeBaseSearchStartegyTypeENUM.Always;

        // Specific Keyword
        public List<string>? SpecificKeywords { get; set; } = null;

        // LLM
        public BusinessAppAgentKnowledgeBaseLLMSearchStrategy? LLMClassifier { get; set; } = null;
    }

    public class BusinessAppAgentKnowledgeBaseLLMSearchStrategy
    {
        public bool UseAgentLLM { get; set; } = true;
        public BusinessAppAgentIntegrationData? LLMIntegration { get; set; } = null;
    }
}