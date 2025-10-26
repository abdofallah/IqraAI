using IqraCore.Entities.Conversation.Enum;

namespace IqraCore.Entities.Conversation.PostAnalysis
{
    public class ConversationPostAnalysis
    {
        public ConversationPostAnalysisStatusEnum Status { get; set; } = ConversationPostAnalysisStatusEnum.NotSet;

        public string? Summary { get; set; } = null;
        public List<ConversationPostAnalsysisTaggingResultData>? Tags { get; set; } = null;
        public List<ConversationPostAnalsysisExtractionFieldResultData>? ExtractedFields { get; set; } = null;
    }
}
