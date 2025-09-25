namespace IqraInfrastructure.Managers.VAD
{
    public class VadStateTracker
    {
        public event Action<TimeSpan>? SpeechStarted;
        public event Action<TimeSpan>? SpeechEnded;

        // Configuration
        private readonly float _threshold;
        private readonly int _minSilenceSamples;
        private readonly int _minSpeechSamples;
        private readonly int _speechPadSamples;
        private static int _windowSizeSamples = 512; // Silero window size

        // State
        private bool _isCurrentlySpeaking = false;
        private bool _triggered = false;
        private int _speechDurationSamples = 0;
        private int _silenceDurationSamples = 0;
        private int _tempEndSamples = 0;

        public VadStateTracker(VadTrackerOptions options)
        {
            _threshold = options.Threshold;
            _minSilenceSamples = (16000 * options.MinSilenceDurationMs) / 1000;
            _minSpeechSamples = (16000 * options.MinSpeechDurationMs) / 1000;
            _speechPadSamples = (16000 * options.SpeechPadMs) / 1000;
        }

        public void ProcessProbability(float speechProbability, TimeSpan durationTimespan)
        {
            if (speechProbability >= _threshold)
            {
                _tempEndSamples = 0;
                _speechDurationSamples += _windowSizeSamples;

                if (_speechDurationSamples >= _minSpeechSamples)
                {
                    _triggered = true;
                    if (!_isCurrentlySpeaking)
                    {
                        _isCurrentlySpeaking = true;
                        SpeechStarted?.Invoke(durationTimespan);
                    }
                    _silenceDurationSamples = 0;
                }
            }
            else // Below threshold
            {
                _silenceDurationSamples += _windowSizeSamples;
                if (_triggered && _silenceDurationSamples > _speechPadSamples)
                {
                    _tempEndSamples = _silenceDurationSamples;
                }

                int effectiveSilence = _tempEndSamples > 0 ? _tempEndSamples : _silenceDurationSamples;

                if (_isCurrentlySpeaking && effectiveSilence >= _minSilenceSamples)
                {
                    _isCurrentlySpeaking = false;
                    _triggered = false;
                    _tempEndSamples = 0;
                    _speechDurationSamples = 0;
                    SpeechEnded?.Invoke(durationTimespan);
                }
            }
        }

        public void Reset()
        {
            _isCurrentlySpeaking = false;
            _triggered = false;
            _speechDurationSamples = 0;
            _silenceDurationSamples = 0;
            _tempEndSamples = 0;
        }
    }

    // A simple config class for the tracker
    public class VadTrackerOptions
    {
        public float Threshold { get; set; } = 0.5f;
        public int MinSilenceDurationMs { get; set; }
        public int MinSpeechDurationMs { get; set; }
        public int SpeechPadMs { get; set; } = 0;
    }
}
