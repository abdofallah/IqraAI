using IqraCore.Entities.Conversation.Enum;

namespace IqraCore.Entities.Conversation
{
    public class ConversationMemberAudioInfo
    {
        public int SampleRate { get; set; }
        public int Channels { get; set; }
        public int BitsPerSample { get; set; }
        public ConversationMemberAudioCompilationStatus AudioCompilationStatus { get; set; } = ConversationMemberAudioCompilationStatus.WaitingForSessionEnd;
        public string? FailedReason { get; set; } = null;
    }
}
