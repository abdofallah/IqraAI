using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;

namespace IqraCore.Interfaces.AI
{
    public interface ISTTService
    {
        Task<FunctionReturnResult> Initialize();
        void StartTranscription();
        void StopTranscription();
        void WriteTranscriptionAudioData(byte[] data);
        event EventHandler<string> TranscriptionResultReceived;
        event EventHandler<string> OnRecoginizingRecieved;
        event EventHandler<object> OnRecoginizingCancelled;

        string GetProviderFullName();
        InterfaceSTTProviderEnum GetProviderType() => GetProviderTypeStatic();
        static InterfaceSTTProviderEnum GetProviderTypeStatic()
        {
            return InterfaceSTTProviderEnum.Unknown;
        }
    }
}