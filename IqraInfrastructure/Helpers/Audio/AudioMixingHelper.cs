using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.TTS;
using IqraInfrastructure.Managers.Conversation.Session.Agent.AI.Helpers; // For AudioFormatDetails
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace IqraInfrastructure.Helpers.Audio
{
    public static class AudioMixingHelper
    {
        /// <summary>
        /// Generates a byte array representing silence for the specified duration and format.
        /// </summary>
        public static byte[] GenerateSilence(int durationMs, int sampleRate, int bitsPerSample, int channels)
        {
            int bytesPerSample = bitsPerSample / 8;
            int totalBytes = (sampleRate * durationMs / 1000) * bytesPerSample * channels;

            // C# arrays are initialized to 0 (silence) by default.
            return new byte[totalBytes];
        }

        /// <summary>
        /// Checks if the input audio matches the target master format. If not, resamples it.
        /// This handles the "Out of Sync" edge case where a client/agent hasn't updated yet.
        /// </summary>
        public static byte[] ResampleIfFormatMismatch(
            byte[] inputData,
            AudioFormatDetails inputFormat,
            AudioFormatDetails targetMasterFormat)
        {
            if (inputData == null || inputData.Length == 0) return Array.Empty<byte>();

            // 1. Fast check: If formats match, return original data (no copy needed)
            if (inputFormat.SampleRate == targetMasterFormat.SampleRate &&
                inputFormat.BitsPerSample == targetMasterFormat.BitsPerSample
            ) {
                return inputData;
            }

            // 2. Conversion Required
            var sourceReq = new TTSProviderAvailableAudioFormat
            {
                Encoding = AudioEncodingTypeEnum.PCM,
                SampleRateHz = inputFormat.SampleRate,
                BitsPerSample = inputFormat.BitsPerSample
            };

            var targetReq = new AudioRequestDetails
            {
                RequestedEncoding = AudioEncodingTypeEnum.PCM,
                RequestedSampleRateHz = targetMasterFormat.SampleRate,
                RequestedBitsPerSample = targetMasterFormat.BitsPerSample
            };

            // Use existing helper
            var (convertedData, _) = AudioConversationHelper.Convert(inputData, sourceReq, targetReq, false);
            return convertedData;
        }

        /// <summary>
        /// Mixes multiple PCM byte arrays into a single Master Mix.
        /// Performs summing and clamping to prevent distortion.
        /// Assumes all inputs are already in the Master Format (use ResampleIfFormatMismatch first).
        /// </summary>
        public static byte[] MixMultiple(List<byte[]> inputs, int bitsPerSample)
        {
            if (inputs == null || inputs.Count == 0) return Array.Empty<byte>();
            if (inputs.Count == 1) return inputs[0]; // Optimization

            // Determine maximum length to allocate result buffer
            int maxLength = 0;
            foreach (var input in inputs)
            {
                if (input.Length > maxLength) maxLength = input.Length;
            }

            byte[] mixedOutput = new byte[maxLength];

            // Route to specific bit-depth logic for performance
            if (bitsPerSample == 16)
            {
                Mix16Bit(inputs, mixedOutput);
            }
            else if (bitsPerSample == 32)
            {
                Mix32BitFloat(inputs, mixedOutput); // Standard WebRTC/High-Def
            }
            else if (bitsPerSample == 8)
            {
                Mix8Bit(inputs, mixedOutput);
            }
            else
            {
                throw new NotSupportedException($"Mixing {bitsPerSample}-bit audio is not supported.");
            }

            return mixedOutput;
        }

        /// <summary>
        /// Performs the "Mix-Minus" operation: MasterMix - SourceAudio.
        /// Used so a speaker doesn't hear themselves.
        /// </summary>
        public static byte[] Subtract(byte[] masterMix, byte[] sourceToRemove, int bitsPerSample)
        {
            if (sourceToRemove == null || sourceToRemove.Length == 0) return masterMix;

            // If the source was silent (all zeros), subtracting it changes nothing.
            // (Optional optimization: check if sourceToRemove is silence, but byte check might be slower than just math)

            byte[] result = new byte[masterMix.Length];

            if (bitsPerSample == 16)
            {
                Subtract16Bit(masterMix, sourceToRemove, result);
            }
            else if (bitsPerSample == 32)
            {
                Subtract32BitFloat(masterMix, sourceToRemove, result);
            }
            else
            {
                // Fallback for 8-bit or 24-bit logic if implemented
                Array.Copy(masterMix, result, masterMix.Length);
            }

            return result;
        }

        #region Unsafe High-Performance Math

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void Mix16Bit(List<byte[]> inputs, byte[] output)
        {
            int sampleCount = output.Length / 2;

            fixed (byte* outPtr = output)
            {
                short* outSamples = (short*)outPtr;

                // Zero out buffer first? No, we can overwrite but summing requires init.
                // Actually, new byte[] is zeroed.

                // We iterate sample by sample to sum all inputs
                // For very large N inputs, this could be optimized by summing 4 at a time, but this is sufficient.
                for (int i = 0; i < sampleCount; i++)
                {
                    int sum = 0;
                    bool hasData = false;

                    foreach (var input in inputs)
                    {
                        if (i * 2 < input.Length)
                        {
                            // Read 16-bit sample safely
                            // Note: This assumes Little Endian (Standard for WAV/PCM)
                            short sample = (short)(input[i * 2] | (input[i * 2 + 1] << 8));
                            sum += sample;
                            hasData = true;
                        }
                    }

                    if (hasData)
                    {
                        // Clamp to 16-bit range
                        if (sum > short.MaxValue) sum = short.MaxValue;
                        else if (sum < short.MinValue) sum = short.MinValue;

                        outSamples[i] = (short)sum;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void Subtract16Bit(byte[] master, byte[] source, byte[] output)
        {
            int sampleCount = master.Length / 2;
            int sourceLimit = source.Length / 2;

            fixed (byte* mPtr = master)
            fixed (byte* sPtr = source)
            fixed (byte* oPtr = output)
            {
                short* mSamples = (short*)mPtr;
                short* sSamples = (short*)sPtr;
                short* oSamples = (short*)oPtr;

                for (int i = 0; i < sampleCount; i++)
                {
                    int mVal = mSamples[i];
                    int sVal = (i < sourceLimit) ? sSamples[i] : 0;

                    int result = mVal - sVal;

                    // Clamp (though logically subtraction shouldn't overflow if it was just added, 
                    // but clipping might have occurred during Mix).
                    if (result > short.MaxValue) result = short.MaxValue;
                    else if (result < short.MinValue) result = short.MinValue;

                    oSamples[i] = (short)result;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void Mix32BitFloat(List<byte[]> inputs, byte[] output)
        {
            int sampleCount = output.Length / 4;

            fixed (byte* outPtr = output)
            {
                float* outSamples = (float*)outPtr;

                for (int i = 0; i < sampleCount; i++)
                {
                    float sum = 0.0f;

                    foreach (var input in inputs)
                    {
                        if (i * 4 < input.Length)
                        {
                            // Convert bytes to float
                            // We cannot just cast pointers if the input alignment is risky, 
                            // but for byte[] buffers in C#, pinned pointers are safe.
                            float sample = BitConverter.ToSingle(input, i * 4);
                            sum += sample;
                        }
                    }

                    // Clamp -1.0 to 1.0
                    if (sum > 1.0f) sum = 1.0f;
                    else if (sum < -1.0f) sum = -1.0f;

                    outSamples[i] = sum;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void Subtract32BitFloat(byte[] master, byte[] source, byte[] output)
        {
            int sampleCount = master.Length / 4;
            int sourceLen = source.Length;

            fixed (byte* mPtr = master)
            fixed (byte* sPtr = source)
            fixed (byte* oPtr = output)
            {
                float* mSamples = (float*)mPtr;
                float* oSamples = (float*)oPtr;
                // We access source via byte index/BitConverter to be safe on bounds inside loop

                for (int i = 0; i < sampleCount; i++)
                {
                    float mVal = mSamples[i];
                    float sVal = 0.0f;

                    if (i * 4 < sourceLen)
                    {
                        sVal = BitConverter.ToSingle(source, i * 4);
                    }

                    float result = mVal - sVal;

                    if (result > 1.0f) result = 1.0f;
                    else if (result < -1.0f) result = -1.0f;

                    oSamples[i] = result;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Mix8Bit(List<byte[]> inputs, byte[] output)
        {
            // 8-bit PCM is usually unsigned (0-255, silence at 128)
            // Implementation is rarer but follows same logic:
            // Convert to int (-128 to 127), sum, clamp, convert back.
            // Placeholder for now as 8-bit mixing is low priority for high-def systems.

            // Simple copy of first input for safety fallback
            if (inputs.Count > 0)
                Array.Copy(inputs[0], output, Math.Min(inputs[0].Length, output.Length));
        }

        #endregion
    }
}