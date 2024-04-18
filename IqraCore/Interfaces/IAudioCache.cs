using System;

namespace IqraCore.Interfaces
{
    public interface IAudioCache
    {
        void SetAudioData(ulong textHash, string ttsProvider, string language, string speaker, byte[] audioData);
        byte[]? GetAudioData(ulong textHash, string ttsProvider, string language, string speaker);
    }
}