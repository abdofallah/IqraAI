namespace IqraCore.Entities.Conversation.Configuration
{
    public class ConversationAgentConfiguration
    {
        public long BusinessId { get; set; }
        public string RouteId { get; set; }
        
        // Audio Config
        public int SampleRate { get; set; }
        public int BitsPerSample { get; set; }
        public int Channels { get; set; }
    }
}
