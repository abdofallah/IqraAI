namespace IqraInfrastructure.Managers.Conversation.Session.Mixer
{
    public static class AudioDSPHelper
    {
        // Thresholds for Silence Detection
        private const double SILENCE_THRESHOLD_16BIT = 300.0; // Max is 32768
        private const double SILENCE_THRESHOLD_32BIT = 0.005; // Max is 1.0 (assuming 32-bit float)

        /// <summary>
        /// Calculates the RMS of the audio chunk and returns true if it's considered silence/background noise.
        /// </summary>
        public static bool IsSilence(byte[] audioData, int bitsPerSample)
        {
            if (audioData == null || audioData.Length == 0) return true;

            double sumSquares = 0;
            int sampleCount = 0;

            if (bitsPerSample == 16)
            {
                sampleCount = audioData.Length / 2;
                for (int i = 0; i < audioData.Length - 1; i += 2)
                {
                    short sample = BitConverter.ToInt16(audioData, i);
                    sumSquares += sample * sample;
                }
                double rms = Math.Sqrt(sumSquares / Math.Max(1, sampleCount));
                return rms < SILENCE_THRESHOLD_16BIT;
            }
            else if (bitsPerSample == 32)
            {
                sampleCount = audioData.Length / 4;
                for (int i = 0; i < audioData.Length - 3; i += 4)
                {
                    float sample = BitConverter.ToSingle(audioData, i);
                    sumSquares += sample * sample;
                }
                double rms = Math.Sqrt(sumSquares / Math.Max(1, sampleCount));
                return rms < SILENCE_THRESHOLD_32BIT;
            }

            return false; // If unknown format, assume it's not silence to avoid dropping.
        }
    }
}
