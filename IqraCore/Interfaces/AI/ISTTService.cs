using IqraCore.Entities.Interfaces;
using System;

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
        InterfaceSTTProviderEnum GetProviderType()
        {
            return InterfaceSTTProviderEnum.Unknown;
        }
    }
}