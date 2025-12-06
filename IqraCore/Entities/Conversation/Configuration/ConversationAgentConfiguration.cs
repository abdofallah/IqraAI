using IqraCore.Entities.Helper.Audio;

namespace IqraCore.Entities.Conversation.Configuration
{
    public class ConversationAgentConfiguration
    {
        public int SampleRate { get; set; }
        public int BitsPerSample { get; set; }
        public int Channels { get; set; } = 1;
        public AudioEncodingTypeEnum AudioEncodingType { get; set; }
        public AudioEncoderFallbackOptimizationMode AudioEncodingFallbackMode { get; set; }
    }
}
