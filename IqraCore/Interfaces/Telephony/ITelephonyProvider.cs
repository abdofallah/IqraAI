using IqraCore.Entities.Call;
using IqraCore.Entities.Helper.Telephony;

namespace IqraCore.Interfaces.Telephony
{
    public interface ITelephonyProvider
    {
        Task<CallProviderResponse> InitiateCallSession(CallQueueData queueData);
        Task<CallProviderResponse> SendAudio(string callId, byte[] audioData);
        Task EndCall(string callId);
        Task PauseAudio(string callId);
        Task ResumeAudio(string callId);

        // Factory method pattern for provider type
        static TelephonyProviderEnum GetProviderType() => TelephonyProviderEnum.Unknown;
    }
}
