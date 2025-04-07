namespace IqraCore.Interfaces.VAD
{
    public interface IVadService : IDisposable
    {
        event EventHandler<VadEventArgs> VoiceActivityChanged; // Single event is often simpler
        void Initialize(string modelPath, VadOptions options);
        void ProcessAudio(ReadOnlyMemory<byte> pcm16AudioChunk);
        void Reset();
    }

    public class VadOptions
    {
        public int SampleRate { get; set; } = 16000; // Default to 16kHz
        public float Threshold { get; set; } = 0.5f; // Speech probability threshold
        public int MinSilenceDurationMs { get; set; } = 300; // Min duration of silence to confirm end of speech
        public int MinSpeechDurationMs { get; set; } = 150; // Min duration of speech to confirm start
        public int SpeechPadMs { get; set; } = 30; // Keep classifying as speech for this long after threshold drops
        public int WindowSizeSamples { get; set; } = 1536; // Model-dependent, e.g., 1536 for 16kHz. Set 0 to use default based on sample rate in Initialize.
    }

    public class VadEventArgs : EventArgs
    {
        public bool IsSpeechDetected { get; }

        public VadEventArgs(bool isSpeechDetected)
        {
            IsSpeechDetected = isSpeechDetected;
        }
    }
}
