using System;

namespace IqraCore.Interfaces
{
    public interface ISTTService
    {
        void Initialize();
        void StartTranscription();
        void StopTranscription();
        void WriteTranscriptionAudioData(byte[] data);
        event EventHandler<string> TranscriptionResultReceived;
        event EventHandler<object> OnRecoginizingRecieved;
    }
}