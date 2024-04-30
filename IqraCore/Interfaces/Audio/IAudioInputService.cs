namespace IqraCore.Interfaces
{
    public interface IAudioInputService
    {
        void Initialize();
        void StartRecording();
        void StopRecording();
        void ClearBuffer();
        event EventHandler<(byte[], int)> AudioDataReceived;
    }
}