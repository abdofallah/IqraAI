namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentKnowledgeBase
    {
        public List<string> LinkedGroups { get; set; } = new List<string>();
        public BusinessAppAgentKnowledgeBaseSearchStrategy SearchStrategy { get; set; } = new BusinessAppAgentKnowledgeBaseSearchStrategy();
        public BusinessAppAgentKnowledgeBaseRefinement Refinement { get; set; } = new BusinessAppAgentKnowledgeBaseRefinement();
    }
}
