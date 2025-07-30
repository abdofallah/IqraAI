using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Helper.Audio;

namespace IqraCore.Entities.Conversation
{
    public class ConversationMemberAudioInfo
    {
        public AudioEncodingTypeEnum AudioEncodingType { get; set; }
        public int SampleRate { get; set; }
        public int Channels { get; set; }
        public int BitsPerSample { get; set; }
        public ConversationMemberAudioCompilationStatus AudioCompilationStatus { get; set; } = ConversationMemberAudioCompilationStatus.WaitingForSessionEnd;
        public string? FailedReason { get; set; } = null;
    }
}
