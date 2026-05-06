using IqraCore.Entities.Conversation.Enum;

namespace IqraCore.Entities.Conversation.PostAnalysis
{
    public class ConversationPostAnalysis
    {
        public ConversationPostAnalysisStatusEnum Status { get; set; } = ConversationPostAnalysisStatusEnum.NotSet;

        public string? PostAnalysisId { get; set; } = null;

        public ConversationSummaryGenerationData SummaryData { get; set; } = new();
        public ConversationPostAnalsysisTaggingData TagsData { get; set; } = new();
        public ConversationPostAnalsysisExtractionFieldData ExtractedFieldsData { get; set; } = new();
    }

    public class ConversationPostAnalsysisTaggingData
    {
        public ConversationPostAnalysisStatusEnum Status { get; set; } = ConversationPostAnalysisStatusEnum.NotSet;

        public List<ConversationPostAnalsysisTaggingResultData>? Tags { get; set; } = null;
    }

    public class ConversationPostAnalsysisExtractionFieldData
    {
        public ConversationPostAnalysisStatusEnum Status { get; set; } = ConversationPostAnalysisStatusEnum.NotSet;

        public List<ConversationPostAnalsysisExtractionFieldResultData>? ExtractedFields { get; set; } = null;
    }

    public class ConversationSummaryGenerationData
    {
        public ConversationPostAnalysisStatusEnum Status { get; set; } = ConversationPostAnalysisStatusEnum.NotSet;

        public ConversationSummaryGenerationResultData? Summary { get; set; } = null;
    }
}
