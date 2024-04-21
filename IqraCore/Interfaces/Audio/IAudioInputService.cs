namespace IqraCore.Interfaces
{
    public interface IAudioInputService
    {
        void Initialize();
        void StartRecording();
        void StopRecording();
        event EventHandler<(byte[], int)> AudioDataReceived;
    }
}