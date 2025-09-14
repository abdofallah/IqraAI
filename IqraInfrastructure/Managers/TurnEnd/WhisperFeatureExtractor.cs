using TorchSharp;
using static TorchSharp.torch;

namespace IqraInfrastructure.Managers.TurnEnd
{
    public class WhisperFeatureExtractor
    {
        // --- Constants ---
        private const int SamplingRate = 16000;
        private const int Nfft = 400;
        private const int HopLength = 160;
        private const int NumMelFilters = 80;
        private const double MinFrequency = 0.0;
        private const double MaxFrequency = 8000.0;

        // --- Properties based on chunk length ---
        private readonly int _chunkLength;
        private readonly int _nSamples;

        private readonly Tensor _melFiltersTensor;

        /// <summary>
        /// Initializes a new instance of the WhisperFeatureExtractor.
        /// [MODIFIED] Now takes chunk_length to be flexible.
        /// </summary>
        /// <param name="chunkLengthInSeconds">The desired audio chunk length in seconds.</param>
        public WhisperFeatureExtractor(int chunkLengthInSeconds = 8)
        {
            _chunkLength = chunkLengthInSeconds;
            _nSamples = _chunkLength * SamplingRate;

            float[,] melFilters = CreateMelFilterBank();
            _melFiltersTensor = from_array(melFilters);
        }

        public float[,] Process(float[] audio, int originalAudioLength)
        {
            // 1. Pad audio to the required number of samples
            var paddedAudio = PadOrTruncate(audio, _nSamples);

            // 2. Apply Zero-Mean Unit-Variance Normalization
            var normalizedAudio = ZeroMeanUnitVarNormalization(paddedAudio);

            // 3. Convert normalized audio to a Torch tensor
            var waveform = tensor(normalizedAudio);

            // 4. Compute STFT
            var window = hann_window(Nfft);
            var stft = torch.stft(waveform, Nfft, HopLength, window: window, return_complex: true);

            // 5. Calculate power spectrum (magnitudes)
            // The slice [.., ..-1] is crucial to match the Python output shape
            long lastDimSize = stft.shape[stft.dim() - 1];
            var slicedStft = stft[.., ..((Index)(lastDimSize - 1))];
            var magnitudes = slicedStft.abs().pow(2);

            // 6. Apply the Mel filter bank via matrix multiplication
            var melSpec = _melFiltersTensor.T.matmul(magnitudes);

            // 7. Compute Log-Mel Spectrogram
            var logSpec = melSpec.clamp_min(1e-10f).log10();

            // 8. Dynamic range compression and normalization
            logSpec = torch.maximum(logSpec, logSpec.max() - 8.0);
            logSpec = (logSpec + 4.0) / 4.0;

            // 9. Convert final tensor back to a C# 2D array
            return To2DArray(logSpec);
        }

        #region Core Processing Steps

        // UNTESTED - prolly correct
        private float[] PadOrTruncate(float[] audio, int targetLength)
        {
            // This assumes padding at the start, which matches your Python script
            var result = new float[targetLength];
            int padding = Math.Max(0, targetLength - audio.Length);
            int lengthToCopy = Math.Min(audio.Length, targetLength);
            int sourceStartIndex = Math.Max(0, audio.Length - targetLength);

            Array.Copy(audio, sourceStartIndex, result, padding, lengthToCopy);
            return result;
        }


        // CORRECT
        public float[] ZeroMeanUnitVarNormalization(float[] paddedAudio, int validLength = 0, float paddingValue = 0.0f)
        {
            // If no valid length specified, assume entire array is valid
            if (validLength <= 0 || validLength > paddedAudio.Length)
            {
                validLength = paddedAudio.Length;
            }

            // --- Step 1: Calculate Mean from valid portion only (equivalent to vector[:length]) ---
            double sum = 0.0;
            for (int i = 0; i < validLength; i++)
            {
                sum += paddedAudio[i];
            }
            float mean = (float)(sum / validLength);

            // --- Step 2: Calculate Variance from valid portion only ---
            double sumOfSquares = 0.0;
            for (int i = 0; i < validLength; i++)
            {
                sumOfSquares += (paddedAudio[i] - mean) * (paddedAudio[i] - mean);
            }
            float variance = (float)(sumOfSquares / validLength);
            float stdDev = (float)Math.Sqrt(variance + 1e-7f);

            // --- Step 3: Apply normalization to entire array ---
            var normalizedAudio = new float[paddedAudio.Length];
            for (int i = 0; i < paddedAudio.Length; i++)
            {
                normalizedAudio[i] = (paddedAudio[i] - mean) / stdDev;
            }

            // --- Step 4: Restore padding values (equivalent to normed_slice[length:] = padding_value) ---
            for (int i = validLength; i < normalizedAudio.Length; i++)
            {
                normalizedAudio[i] = paddingValue;
            }

            return normalizedAudio;
        }

        // CORRECT
        private float[,] To2DArray(Tensor tensor)
        {
            long rows = tensor.shape[0];
            long cols = tensor.shape[1];
            var result = new float[rows, cols];
            var data = tensor.data<float>();

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    result[i, j] = data[i * cols + j];
                }
            }
            return result;
        }

        #endregion

        #region Mel Filter Bank Creation

        private float[,] CreateMelFilterBank()
        {
            int numFreqBins = 1 + Nfft / 2;

            // 1. Calculate Mel frequency points using the Slaney scale
            double melMin = HzToMelSlaney(MinFrequency);
            double melMax = HzToMelSlaney(MaxFrequency);

            var melPoints = new double[NumMelFilters + 2];
            double melSpacing = (melMax - melMin) / (NumMelFilters + 1);
            for (int i = 0; i < melPoints.Length; i++)
            {
                melPoints[i] = melMin + i * melSpacing;
            }

            var filterFreqs = melPoints.Select(m => (float)MelToHzSlaney(m)).ToArray();

            // 2. Calculate FFT frequency points
            var fftFreqs = new float[numFreqBins];
            float fftBinWidth = (float)SamplingRate / Nfft;
            for (int i = 0; i < numFreqBins; i++)
            {
                fftFreqs[i] = i * fftBinWidth;
            }

            // 3. Create triangular filter bank
            var melFilters = CreateTriangularFilterBank(fftFreqs, filterFreqs);

            // 4. Apply Slaney normalization
            var enorm = new float[NumMelFilters];
            for (int i = 0; i < NumMelFilters; i++)
            {
                enorm[i] = 2.0f / (filterFreqs[i + 2] - filterFreqs[i]);
            }

            for (int i = 0; i < numFreqBins; i++)
            {
                for (int j = 0; j < NumMelFilters; j++)
                {
                    melFilters[i, j] *= enorm[j];
                }
            }

            return melFilters;
        }

        private float[,] CreateTriangularFilterBank(float[] fftFreqs, float[] filterFreqs)
        {
            var filterDiff = new float[filterFreqs.Length - 1];
            for (int i = 0; i < filterDiff.Length; i++)
            {
                filterDiff[i] = filterFreqs[i + 1] - filterFreqs[i];
            }

            var slopes = new float[fftFreqs.Length, filterFreqs.Length];
            for (int i = 0; i < fftFreqs.Length; i++)
            {
                for (int j = 0; j < filterFreqs.Length; j++)
                {
                    slopes[i, j] = filterFreqs[j] - fftFreqs[i];
                }
            }

            var melFilters = new float[fftFreqs.Length, NumMelFilters];
            for (int i = 0; i < fftFreqs.Length; i++)
            {
                for (int j = 0; j < NumMelFilters; j++)
                {
                    float downSlope = -slopes[i, j] / filterDiff[j];
                    float upSlope = slopes[i, j + 2] / filterDiff[j + 1];
                    melFilters[i, j] = Math.Max(0, Math.Min(downSlope, upSlope));
                }
            }

            return melFilters;
        }

        private double HzToMelSlaney(double hz)
        {
            const double minLogHertz = 1000.0;
            if (hz < minLogHertz)
            {
                return 3.0 * hz / 200.0;
            }
            const double minLogMel = 15.0;
            const double logstep = 27.0 / 1.8552668351659398; // 27.0 / log(6.4)
            return minLogMel + Math.Log(hz / minLogHertz) * logstep;
        }

        private double MelToHzSlaney(double mel)
        {
            const double minLogMel = 15.0;
            if (mel < minLogMel)
            {
                return 200.0 * mel / 3.0;
            }
            const double minLogHertz = 1000.0;
            const double logstep = 0.06875177742094911; // log(6.4) / 27.0
            return minLogHertz * Math.Exp(logstep * (mel - minLogMel));
        }

        #endregion
    }
}