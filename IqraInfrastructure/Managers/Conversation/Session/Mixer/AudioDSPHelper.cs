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

        /// <summary>
        /// Takes two identical-length chunks of audio and crossfades them into a single chunk.
        /// This effectively halves the duration (shrinks) without causing a harsh clicking artifact.
        /// </summary>
        public static byte[] ShrinkAudio(byte[] chunk1, byte[] chunk2, int bitsPerSample)
        {
            if (chunk1 == null || chunk2 == null || chunk1.Length != chunk2.Length)
            {
                // Fallback: just return chunk 2 if sizes mismatch
                return chunk2 ?? chunk1 ?? Array.Empty<byte>();
            }

            byte[] result = new byte[chunk1.Length];

            if (bitsPerSample == 16)
            {
                int sampleCount = chunk1.Length / 2;
                for (int i = 0; i < sampleCount; i++)
                {
                    int offset = i * 2;
                    short sample1 = BitConverter.ToInt16(chunk1, offset);
                    short sample2 = BitConverter.ToInt16(chunk2, offset);

                    // Linear crossfade: factor goes from 0.0 to 1.0
                    float factor = (float)i / sampleCount;
                    short blended = (short)((sample1 * (1.0f - factor)) + (sample2 * factor));

                    byte[] blendedBytes = BitConverter.GetBytes(blended);
                    result[offset] = blendedBytes[0];
                    result[offset + 1] = blendedBytes[1];
                }
            }
            else if (bitsPerSample == 32)
            {
                int sampleCount = chunk1.Length / 4;
                for (int i = 0; i < sampleCount; i++)
                {
                    int offset = i * 4;
                    float sample1 = BitConverter.ToSingle(chunk1, offset);
                    float sample2 = BitConverter.ToSingle(chunk2, offset);

                    float factor = (float)i / sampleCount;
                    float blended = (sample1 * (1.0f - factor)) + (sample2 * factor);

                    byte[] blendedBytes = BitConverter.GetBytes(blended);
                    Buffer.BlockCopy(blendedBytes, 0, result, offset, 4);
                }
            }
            else
            {
                // Fallback for unsupported bit depth: return the newest chunk
                Buffer.BlockCopy(chunk2, 0, result, 0, chunk2.Length);
            }

            return result;
        }
    }
}
