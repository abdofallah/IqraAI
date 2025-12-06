using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.WebSession;

namespace IqraCore.Entities.Conversation.Configuration
{
    public abstract class ConversationClientConfiguration
    {
        public int SampleRate { get; set; }
        public int BitsPerSample { get; set; }
        public int Channels { get; set; } = 1;
        public AudioEncodingTypeEnum AudioEncodingType { get; set; }
        public AudioEncoderFallbackOptimizationMode AudioEncodingFallbackMode { get; set; }
        public int FrameDurationMs { get; set; }
        public int MaxBufferAheadMs { get; set; }
        public int InitialSegmentDurationMs { get; set; }
    }

    public class ConversationTelephonyClientConfiguration : ConversationClientConfiguration
    {
        public CallQueueData QueueData { get; set; }
    }

    public class ConversationWebClientConfiguration : ConversationClientConfiguration
    {
        public WebSessionData WebSessionData { get; set; }
    }
}
