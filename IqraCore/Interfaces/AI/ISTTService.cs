using IqraCore.Entities.Interfaces;

namespace IqraCore.Interfaces.AI
{
    public interface ISTTService
    {
        void Initialize();
        void StartTranscription();
        void StopTranscription();
        void WriteTranscriptionAudioData(byte[] data);
        event EventHandler<string> TranscriptionResultReceived;
        event EventHandler<object> OnRecoginizingRecieved;

        string GetProviderFullName();
        InterfaceSTTProviderEnum GetProviderType();
        static InterfaceSTTProviderEnum GetProviderTypeStatic()
        {
            return InterfaceSTTProviderEnum.Unknown;
        }
    }
}