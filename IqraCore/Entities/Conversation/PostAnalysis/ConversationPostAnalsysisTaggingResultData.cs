namespace IqraCore.Entities.Conversation.PostAnalysis
{
    public class ConversationPostAnalsysisTaggingResultData
    {
        public string Thinking { get; set; }
        public string TagId { get; set; }
        public List<ConversationPostAnalsysisTaggingResultData> SubTags { get; set; }
    }
}
