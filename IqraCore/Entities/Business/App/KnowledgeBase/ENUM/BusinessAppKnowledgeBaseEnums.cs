namespace IqraCore.Entities.Business.App.KnowledgeBase.ENUM
{
    public enum KnowledgeBaseChunkingType
    {
        General = 0,
        ParentChild = 1
    }

    public enum KnowledgeBaseChunkingParentChunkType
    {
        Paragraph = 0,
        FullDoc = 1
    }

    public enum KnowledgeBaseRetrievalType
    {
        VectorSearch = 0,
        FullTextSearch = 1,
        HybirdSearch = 2
    }

    public enum KnowledgeBaseHybridRetrievalMode
    {
        WeightedScore = 0,
        RerankModel = 1
    }

    public enum KnowledgeBaseDocumentStatus
    {
        Processing = 0,
        Ready = 1,
        Failed = 2
    }

    public enum KnowledgeBaseDocumentType
    {
        General = 0,
        Parent = 1,
        Child = 2
    }
}
