namespace IqraCore.Entities.Conversation.Turn
{
    public class ConversationTurnKnowledgeBaseRetrievalData
    {
        public bool WasSuccessfull { get; set; }

        public string? ResultMessage { get; set; }
        public List<Dictionary<string, object>>? RetrievedResultsMetaData { get; set; }
        public int RetrievalLatencyMS { get; set; }
    }
}
