using IqraCore.Entities.Conversation.Enum;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace IqraCore.Entities.Conversation
{
    public class ConversationMessageData
    {
        public string SenderId { get; set; }
        public ConversationSenderRole Role { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }

        [JsonIgnore]
        [BsonIgnore]
        public bool HasAudioData => !string.IsNullOrEmpty(AudioDataReference);

        public string AudioDataReference { get; set; }
    }

    public class ConversationMessage
    {
        public string SenderId { get; }
        public ConversationSenderRole Role { get; }
        public string Content { get; }
        public DateTime Timestamp { get; }
        public byte[] AudioData { get; }

        public ConversationMessage(string senderId, ConversationSenderRole role, string content, byte[] audioData = null)
        {
            SenderId = senderId;
            Role = role;
            Content = content;
            Timestamp = DateTime.UtcNow;
            AudioData = audioData;
        }
    }
}
