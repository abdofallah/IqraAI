namespace IqraCore.Entities.Conversation.Turn
{
    public class UserInput
    {
        public string SenderId { get; set; }
        public string? TranscribedText { get; set; }
        public DateTime StartedSpeakingAt { get; set; }
        public DateTime? FinishedSpeakingAt { get; set; }
    }
}