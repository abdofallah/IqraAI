using NWaves.Transforms;
using NWaves.Filters.Fda;
using NWaves.Windows;

namespace IqraInfrastructure.Managers.TurnEnd
{
    /// <summary>
    /// Re-implements the core functionality of HuggingFace's WhisperFeatureExtractor
    /// in C# to generate log-Mel spectrograms from raw audio.
    /// </summary>
    public class WhisperFeatureExtractor
    {
        // Configuration constants matching the Whisper model's pre-processing.
        private const int SampleRate = 16000;
        private const int N_Fft = 400;
        private const int HopLength = 160;
        private const int N_Mels = 80;

        private readonly float[][] _melFilters; // Jagged array (float[][]) is the correct type from FilterBanks
        private readonly Stft _stft;

        public WhisperFeatureExtractor()
        {
            // 1. Pre-compute the Mel filter bank using the static FilterBanks class.
            //    The result is a jagged array of shape [N_Mels][N_Fft / 2 + 1].
            _melFilters = FilterBanks.MelBankSlaney(
                filterCount: N_Mels,
                fftSize: N_Fft,
                samplingRate: SampleRate,
                lowFreq: 0.0,
                highFreq: 8000.0,
                normalizeGain: false); // Whisper implementation does not use Slaney's gain normalization.

            // 2. Configure the Short-Time Fourier Transform (STFT) processor.
            _stft = new Stft(N_Fft, HopLength, window: WindowType.Hann);
        }

        /// <summary>
        /// Processes a 16kHz mono audio waveform into a log-Mel spectrogram.
        /// </summary>
        /// <param name="audio">The raw audio samples as a float array.</param>
        /// <returns>A 2D float array of shape [N_Mels, num_frames].</returns>
        public float[,] Process(float[] audio)
        {
            // 1. Compute the STFT. The result is a list where each element is a tuple
            //    containing a float[] for the real parts and a float[] for the imaginary parts of a frame.
            var stftResult = _stft.Direct(audio);
            int frameCount = stftResult.Count;
            int freqBinCount = N_Fft / 2 + 1;

            // 2. Compute the Mel spectrogram directly from the STFT result.
            //    The resulting matrix will have dimensions [frameCount, N_Mels].
            var melSpectrogram = new float[frameCount, N_Mels];

            for (int i = 0; i < frameCount; i++) // For each time frame
            {
                // Deconstruct the tuple to get the real and imaginary arrays for the current frame.
                var (real, imag) = stftResult[i];

                for (int k = 0; k < N_Mels; k++) // For each Mel filter
                {
                    float melEnergy = 0;
                    for (int j = 0; j < freqBinCount; j++) // For each frequency bin
                    {
                        // Correctly calculate power from the separate real and imaginary arrays.
                        float power = (real[j] * real[j]) + (imag[j] * imag[j]);
                        melEnergy += _melFilters[k][j] * power;
                    }
                    melSpectrogram[i, k] = melEnergy;
                }
            }

            // 4. Apply logarithmic scaling. (This part remains correct)
            for (int i = 0; i < frameCount; i++)
            {
                for (int j = 0; j < N_Mels; j++)
                {
                    float value = melSpectrogram[i, j];
                    melSpectrogram[i, j] = (float)Math.Log10(Math.Max(value, 1e-10));
                }
            }

            // 5. Apply dynamic range compression (normalization). (This part remains correct)
            float maxValue = FindMaxValue(melSpectrogram);
            float clipValue = maxValue - 8.0f;

            for (int i = 0; i < frameCount; i++)
            {
                for (int j = 0; j < N_Mels; j++)
                {
                    melSpectrogram[i, j] = Math.Max(melSpectrogram[i, j], clipValue);
                    melSpectrogram[i, j] = (melSpectrogram[i, j] + 4.0f) / 4.0f;
                }
            }

            // 6. Transpose the final matrix to match the model's expected input shape: [N_Mels, frameCount]
            return Transpose(melSpectrogram);
        }

        #region Helper Methods

        private float FindMaxValue(float[,] matrix)
        {
            float max = float.MinValue;
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    if (matrix[i, j] > max)
                    {
                        max = matrix[i, j];
                    }
                }
            }
            return max;
        }

        private float[,] Transpose(float[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            var result = new float[cols, rows];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    result[j, i] = matrix[i, j];
                }
            }
            return result;
        }

        #endregion
    }
}
