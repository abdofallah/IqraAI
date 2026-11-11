using IqraCore.Entities.Conversation.Enum;

namespace IqraCore.Models.Business.Conversations
{
    /// <summary>
    /// 1:1 ViewModel mirroring ConversationPostAnalysis entity.
    /// Tag/field resolution (TagId → name) is done frontend-side via BusinessFullData.
    /// </summary>
    public class ConversationPostAnalysisViewModel
    {
        public ConversationPostAnalysisStatusEnum Status { get; set; }
        public string? PostAnalysisId { get; set; }

        public ConversationSummaryGenerationViewModel SummaryData { get; set; } = new();
        public ConversationTaggingDataViewModel TagsData { get; set; } = new();
        public ConversationExtractionDataViewModel ExtractedFieldsData { get; set; } = new();
    }

    public class ConversationSummaryGenerationViewModel
    {
        public ConversationPostAnalysisStatusEnum Status { get; set; }
        public ConversationSummaryResultViewModel? Summary { get; set; }
    }

    public class ConversationSummaryResultViewModel
    {
        public string Thinking { get; set; } = null!;
        public string Summary { get; set; } = null!;
    }

    public class ConversationTaggingDataViewModel
    {
        public ConversationPostAnalysisStatusEnum Status { get; set; }
        public List<ConversationTagResultViewModel>? Tags { get; set; }
    }

    public class ConversationTagResultViewModel
    {
        public string Thinking { get; set; } = null!;
        public string TagId { get; set; } = null!;
        public List<ConversationTagResultViewModel>? SubTags { get; set; }
    }

    public class ConversationExtractionDataViewModel
    {
        public ConversationPostAnalysisStatusEnum Status { get; set; }
        public List<ConversationExtractionFieldResultViewModel>? ExtractedFields { get; set; }
    }

    public class ConversationExtractionFieldResultViewModel
    {
        public string Thinking { get; set; } = null!;
        public string FieldId { get; set; } = null!;
        public object? FieldValue { get; set; }
        public List<ConversationExtractionFieldResultViewModel>? ConditionalExtractedFields { get; set; }
    }
}
