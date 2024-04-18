namespace IqraCore.Interfaces
{
    public interface IAudioOutputService
    {
        void Initialize();
        void EnqueueAudioData(byte[] data);
        void StartPlayback();
        void StopPlayback();
        bool IsBufferEmpty();
        TimeSpan BufferAudioDataDuration();
        void ClearAudioData();
    }
}