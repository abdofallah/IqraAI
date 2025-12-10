namespace IqraCore.Entities.Conversation.Turn
{
    public class ConversationTurnUserInput
    {
        public string? TranscribedText { get; set; }
        public DateTime StartedSpeakingAt { get; set; }
        public DateTime? FinishedSpeakingAt { get; set; }
    }
}