namespace IqraCore.Entities.Conversation.PostAnalysis
{
    public class ConversationPostAnalsysisTaggingResultData
    {
        public string Thinking { get; set; } = null!;
        public string TagId { get; set; } = null!;
        public List<ConversationPostAnalsysisTaggingResultData>? SubTags { get; set; } = null;
    }
}
