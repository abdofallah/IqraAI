using IqraCore.Entities.Conversation.Enum;

namespace IqraCore.Entities.Conversation.PostAnalysis
{
    public class ConversationPostAnalsysisExtractionFieldResultData
    {
        public string Thinking { get; set; } = null!;
        public string FieldId { get; set; } = null!;
        public object? FieldValue { get; set; } = null;

        public List<ConversationPostAnalsysisExtractionFieldResultData>? ConditionalExtractedFields { get; set; } = null;
    }
}
