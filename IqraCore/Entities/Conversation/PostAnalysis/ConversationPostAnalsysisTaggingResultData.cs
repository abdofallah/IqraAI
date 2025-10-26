namespace IqraCore.Entities.Conversation.PostAnalysis
{
    public class ConversationPostAnalsysisTaggingResultData
    {
        public string Thinking { get; set; }
        public string TagId { get; set; }
        List<ConversationPostAnalsysisTaggingResultData> SubTags { get; set; }
    }
}
