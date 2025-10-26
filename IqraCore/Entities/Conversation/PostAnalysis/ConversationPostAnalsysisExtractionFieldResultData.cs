namespace IqraCore.Entities.Conversation.PostAnalysis
{
    public class ConversationPostAnalsysisExtractionFieldResultData
    {
        public string Thinking { get; set; }
        public string FieldId { get; set; }
        public object FieldValue { get; set; }

        public List<ConversationPostAnalsysisExtractionFieldResultData> ConditionalExtractedFields { get; set; }
    }
}
