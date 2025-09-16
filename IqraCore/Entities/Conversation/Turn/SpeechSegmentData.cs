namespace IqraCore.Entities.Conversation.Turn
{
    public class SpeechSegmentData
    {
        public string Text { get; set; }
        public DateTime StartedPlayingAt { get; set; }
        public DateTime? FinishedPlayingAt { get; set; } // Null if interrupted while playing
        public bool WasInterrupted { get; set; } = false;
        public TimeSpan Duration { get; set; }
    }
}