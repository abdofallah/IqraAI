using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.TTS;

namespace IqraInfrastructure.Managers.TTS.Helpers
{
    public static class AudiEncoderFallbackSelector
    {
        // Profile now includes separate, asymmetric costs for decoding and encoding.
        private record EncodingProfile(
            AudioEncodingTypeEnum Encoding,
            int TypicalSampleRate,
            int TypicalBitsPerSample,
            int DecodingCost, // Cost to convert this format TO PCM
            int EncodingCost  // Cost to convert PCM TO this format
        );

        private record ScoredOption(TTSProviderAvailableAudioFormat Format, int Score);

        // Expanded "knowledge base" with both decoding and encoding costs.
        // Note: Encoding is often more computationally expensive for complex codecs.
        private static readonly Dictionary<AudioEncodingTypeEnum, EncodingProfile> Profiles = new()
        {
            // Raw/Wrapper Formats
            { AudioEncodingTypeEnum.PCM,   new EncodingProfile(AudioEncodingTypeEnum.PCM, 0, 0, 0, 0) }, // The baseline, zero cost
            { AudioEncodingTypeEnum.WAV,   new EncodingProfile(AudioEncodingTypeEnum.WAV, 0, 0, 5, 5) }, // Trivial cost to strip/add header

            // Telephony Companding Formats (Symmetric and cheap)
            { AudioEncodingTypeEnum.MULAW, new EncodingProfile(AudioEncodingTypeEnum.MULAW, 8000, 8, 20, 20) },
            { AudioEncodingTypeEnum.ALAW,  new EncodingProfile(AudioEncodingTypeEnum.ALAW, 8000, 8, 20, 20) },

            // VoIP Codecs (Asymmetric costs)
            { AudioEncodingTypeEnum.G722,  new EncodingProfile(AudioEncodingTypeEnum.G722, 16000, 16, 80, 100) },
            { AudioEncodingTypeEnum.G729,  new EncodingProfile(AudioEncodingTypeEnum.G729, 8000, 16, 100, 130) },
            { AudioEncodingTypeEnum.OPUS,  new EncodingProfile(AudioEncodingTypeEnum.OPUS, 48000, 16, 90, 120) } // Encoding is more complex
        };

        // Penalty points remain the same
        private const int RESAMPLING_BASE_PENALTY = 20;
        private const int RESAMPLE_DISTANCE_MULTIPLIER = 10;
        private const int BIT_DEPTH_CONVERSION_PENALTY = 30;
        private const int PERFORMANCE_UPSAMPLING_PENALTY = 40;
        private const int PERFORMANCE_BIT_EXPANSION_PENALTY = 40;

        // NEW: Extreme penalties for "Quality Mode" to avoid bad conversions.
        private const int QUALITY_UPSAMPLING_PENALTY = 200;
        private const int QUALITY_BIT_EXPANSION_PENALTY = 150;

        public static List<TTSProviderAvailableAudioFormat> GetFallbackOrder(AudioRequestDetails request, IEnumerable<TTSProviderAvailableAudioFormat> availableFormats, AudioEncoderFallbackOptimizationMode audioOptimzationMode = AudioEncoderFallbackOptimizationMode.Quality)
        {
            if (!Profiles.ContainsKey(request.RequestedEncoding))
            {
                throw new NotSupportedException($"The requested target encoding '{request.RequestedEncoding}' is not supported.");
            }

            var scoredOptions = new List<ScoredOption>();
            var targetProfile = Profiles[request.RequestedEncoding];

            foreach (var availableFormat in availableFormats)
            {
                if (!Profiles.ContainsKey(availableFormat.Encoding)) continue;

                var sourceProfile = Profiles[availableFormat.Encoding];
                // Pass the specific availableFormat to the scoring function
                int score = CalculateScore(availableFormat, sourceProfile, targetProfile, request, audioOptimzationMode);
                scoredOptions.Add(new ScoredOption(availableFormat, score));
            }
           
            return scoredOptions.OrderBy(o => o.Score).Select(o => o.Format).ToList();
        }

        private static int CalculateScore(TTSProviderAvailableAudioFormat sourceFormat, EncodingProfile sourceProfile, EncodingProfile targetProfile, AudioRequestDetails request, AudioEncoderFallbackOptimizationMode audioOptimzationMode)
        {
            int baseScore = 0;
            // --- Step 1: Base Score Calculation ---
            // (This logic remains the same, it is already correct)
            if (sourceFormat.Encoding == request.RequestedEncoding)
            { 
                baseScore = 0;
            }
            // Case 2: The special shortcut for Telephony codecs <-> 8-bit, 8kHz PCM.
            // This is a very cheap, direct mathematical conversion, not a full transcode.
            else if ((sourceFormat.Encoding == AudioEncodingTypeEnum.MULAW || sourceFormat.Encoding == AudioEncodingTypeEnum.ALAW) &&
                       request.RequestedEncoding == AudioEncodingTypeEnum.PCM && request.RequestedSampleRateHz == 8000 && request.RequestedBitsPerSample == 8)
            {
                baseScore = 10; // Assign a small, fixed cost for this special case.
            }
            // Case 3: Converting FROM 8-bit, 8kHz PCM to a Telephony codec. Also a cheap shortcut.
            else if (sourceFormat.Encoding == AudioEncodingTypeEnum.PCM && sourceFormat.SampleRateHz == 8000 && sourceFormat.BitsPerSample == 8 &&
                     (request.RequestedEncoding == AudioEncodingTypeEnum.MULAW || request.RequestedEncoding == AudioEncodingTypeEnum.ALAW))
            {
                baseScore = 10;
            }
            else
            {
                // Case 4: Standard transcoding. The cost is decoding the source and encoding the target.
                // This correctly handles WAV->PCM (5+0=5), OPUS->PCM (90+0=90), OPUS->G729 (90+130=220), etc.
                baseScore = sourceProfile.DecodingCost + targetProfile.EncodingCost;
            }

            // --- Step 2: Calculate DYNAMIC Transformation Score ---
            int transformScore = 0;

            // --- Sample Rate Transformation ---
            if (sourceFormat.SampleRateHz != request.RequestedSampleRateHz)
            {
                // Add the small, fixed base penalty.
                transformScore += RESAMPLING_BASE_PENALTY;

                // Calculate the resampling ratio. Use double for precision.
                double resampleRatio = (double)Math.Max(sourceFormat.SampleRateHz, request.RequestedSampleRateHz) /
                                       (double)Math.Min(sourceFormat.SampleRateHz, request.RequestedSampleRateHz);

                // Add a dynamic penalty based on how far apart the rates are.
                // (ratio - 1) ensures no penalty if the ratio is 1 (i.e., they are equal, though this code path isn't hit).
                int distancePenalty = (int)((resampleRatio - 1) * RESAMPLE_DISTANCE_MULTIPLIER);
                transformScore += distancePenalty;

                // Add the EXTRA quality penalty if we are upsampling.
                if (sourceFormat.SampleRateHz < request.RequestedSampleRateHz)
                {
                    transformScore += (audioOptimzationMode == AudioEncoderFallbackOptimizationMode.Quality)
                    ? QUALITY_UPSAMPLING_PENALTY
                    : PERFORMANCE_UPSAMPLING_PENALTY;
                }
            }

            // --- Bit Depth Transformation ---
            if (sourceFormat.BitsPerSample != request.RequestedBitsPerSample)
            {
                transformScore += BIT_DEPTH_CONVERSION_PENALTY;
                if (sourceFormat.BitsPerSample < request.RequestedBitsPerSample)
                {
                    transformScore += (audioOptimzationMode == AudioEncoderFallbackOptimizationMode.Quality)
                    ? QUALITY_BIT_EXPANSION_PENALTY
                    : PERFORMANCE_BIT_EXPANSION_PENALTY;
                }
            }

            return baseScore + transformScore;
        }
    }
}
