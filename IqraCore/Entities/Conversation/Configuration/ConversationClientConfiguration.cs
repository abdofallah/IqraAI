using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Helper.Audio;

namespace IqraCore.Entities.Conversation.Configuration
{
    public class ConversationClientConfiguration
    {
        public CallQueueData QueueData { get; set; }
        
        // Audio Config
        public int SampleRate { get; set; }
        public int BitsPerSample { get; set; }
        public int Channels { get; set; }
        public AudioEncodingTypeEnum AudioEncodingType { get; set; }
    }
}
