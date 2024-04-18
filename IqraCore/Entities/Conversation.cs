using System;

namespace IqraCore.Entities
{
    public class Conversation
    {
        public string ConversationId { get; set; }
        public string SessionId { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserInput { get; set; }
        public string AIResponse { get; set; }
        // Add other relevant properties as needed
    }
}