using Concentus;
using Concentus.Enums;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.TTS;
using NAudio.Codecs;
using NAudio.Mixer;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NLayer.NAudioSupport;
using SIPSorceryMedia.Abstractions;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace IqraInfrastructure.Managers.TTS.Helpers
{
    public static class AudioConversationHelper
    {
        // NOTE: For G.729 support on Linux, the native SDL2 library must be installed.
        // On Debian/Ubuntu: sudo apt-get install -y libsdl2-2.0-0
        private static readonly SIPSorcery.Media.AudioEncoder _audioEncoder = new SIPSorcery.Media.AudioEncoder();

        /// <summary>
        /// Converts audio data from a source format to a target format, handling encoding, sample rate, and bit depth changes.
        /// </summary>
        public static (byte[] audioData, TimeSpan duration) Convert(byte[] sourceAudio, TTSProviderAvailableAudioFormat sourceFormat, AudioRequestDetails targetFormat)
        {
            if (sourceAudio == null || sourceAudio.Length == 0)
            {
                return (Array.Empty<byte>(), TimeSpan.Zero);
            }

            // Optimization: If no conversion is needed at all.
            if (sourceFormat.Encoding == targetFormat.RequestedEncoding &&
                sourceFormat.SampleRateHz == targetFormat.RequestedSampleRateHz &&
                sourceFormat.BitsPerSample == targetFormat.RequestedBitsPerSample)
            {
                // Still calculate duration to be safe, especially for compressed formats.
                TimeSpan simpleDuration = CalculateDuration(sourceAudio, sourceFormat);
                return (sourceAudio, simpleDuration);
            }

            IWaveProvider? pcmProvider = null;
            IWaveProvider? resampledProvider = null;
            try
            {
                // Step 1: Decode the source audio into a standardized 32-bit float PCM IWaveProvider.
                pcmProvider = CreatePcm32FloatProvider(sourceAudio, sourceFormat);          

                // Step 2: Resample if necessary. This now operates on the clean 32-bit float PCM.
                resampledProvider = CreateResampler(pcmProvider, targetFormat);

                // Step 3: Encode the processed PCM stream into the final target format.
                byte[] targetAudio = EncodeToTargetFormat(resampledProvider, targetFormat);

                // Final duration calculation on the converted data.
                var finalFormat = new TTSProviderAvailableAudioFormat
                {
                    Encoding = targetFormat.RequestedEncoding,
                    SampleRateHz = targetFormat.RequestedSampleRateHz,
                    BitsPerSample = targetFormat.RequestedBitsPerSample
                };
                TimeSpan duration = CalculateDuration(targetAudio, finalFormat);

                return (targetAudio, duration);
            }
            catch (Exception ex)
            {
                string errorMessage = $"An error occurred while converting audio from {sourceFormat.Encoding} ({sourceFormat.SampleRateHz}Hz) to {targetFormat.RequestedEncoding} ({targetFormat.RequestedSampleRateHz}Hz).";
                throw new Exception(errorMessage, ex);
            }
            finally
            {
                (pcmProvider as IDisposable)?.Dispose();
                (resampledProvider as IDisposable)?.Dispose();
            }
        }

        /// <summary>
        /// Calculates the duration of audio data based on its format.
        /// </summary>
        public static TimeSpan CalculateDuration(byte[] audioData, TTSProviderAvailableAudioFormat format)
        {
            if (audioData == null || audioData.Length == 0) return TimeSpan.Zero;

            try
            {
                // For WAV, read the header directly. It's the most reliable.
                if (format.Encoding == AudioEncodingTypeEnum.WAV || IsWavFile(audioData))
                {
                    using var memStream = new MemoryStream(FixWavHeader(audioData));
                    using var reader = new WaveFileReader(memStream);
                    return reader.TotalTime;
                }

                // For MP3/MPEG, NLayer can read the duration from the frames.
                if (format.Encoding == AudioEncodingTypeEnum.MPEG)
                {
                    var inputStream = new MemoryStream(audioData);
                    var builder = new Mp3FileReader.FrameDecompressorBuilder(wf => new Mp3FrameDecompressor(wf));
                    var rawProvider = new Mp3FileReaderBase(inputStream, builder);
                    return rawProvider.TotalTime;
                }

                // For raw PCM, we can calculate directly.
                if (format.Encoding == AudioEncodingTypeEnum.PCM)
                {
                    if (format.SampleRateHz <= 0 || format.BitsPerSample <= 0) return TimeSpan.Zero;
                    long totalBytes = audioData.Length;
                    int bytesPerSample = format.BitsPerSample / 8;
                    if (bytesPerSample == 0) return TimeSpan.Zero;
                    int channels = 1; // Assuming mono for this calculation
                    long totalSamples = totalBytes / bytesPerSample / channels;
                    return TimeSpan.FromSeconds((double)totalSamples / format.SampleRateHz);
                }

                // For all other compressed formats (MULAW, ALAW, G729, etc.), the most reliable
                // way to get duration is to decode to PCM and measure the result.
                IWaveProvider? pcmProvider = null;
                try
                {
                    pcmProvider = CreatePcm32FloatProvider(audioData, format);

                    byte[] pcmBytes = ReadAllBytes(pcmProvider);
                    long totalBytes = pcmBytes.Length;

                    // Now, use the reliable formula on the decoded PCM data.
                    if (pcmProvider.WaveFormat.AverageBytesPerSecond == 0) return TimeSpan.Zero;
                    double totalSeconds = (double)totalBytes / pcmProvider.WaveFormat.AverageBytesPerSecond;
                    return TimeSpan.FromSeconds(totalSeconds);
                }
                finally
                {
                    (pcmProvider as IDisposable)?.Dispose();
                }
            }
            catch
            {
                // Fallback for any parsing errors
                return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Mixes two audio chunks, providing fine-grained control over volume and clipping.
        /// </summary>
        public static byte[] MixAudioChunks(
            AudioEncodingTypeEnum encodingType, int sampleRate, int bitsPerSample,
            ReadOnlyMemory<byte> speechChunk, float speechChunkVolume,
            ReadOnlyMemory<byte> backgroundChunk, float backgroundChunkVolume
)
        {
            bool speechIsEmpty = speechChunk.IsEmpty || speechChunk.Length == 0;
            bool backgroundIsEmpty = backgroundChunk.IsEmpty || backgroundChunk.Length == 0;

            if (speechIsEmpty && backgroundIsEmpty) return Array.Empty<byte>();

            var sourceFormat = new TTSProviderAvailableAudioFormat { Encoding = encodingType, SampleRateHz = sampleRate, BitsPerSample = bitsPerSample };

            if (speechIsEmpty)
            {
                return ApplyVolumeToChunk(backgroundChunk.Span, sourceFormat, backgroundChunkVolume);
            }
            if (backgroundIsEmpty)
            {
                return ApplyVolumeToChunk(speechChunk.Span, sourceFormat, speechChunkVolume);
            }

            // Define the target format for our high-precision intermediate stage.
            var pcm32Target = new AudioRequestDetails { RequestedEncoding = AudioEncodingTypeEnum.PCM, RequestedSampleRateHz = sampleRate, RequestedBitsPerSample = 32 };

            var speechConverted = Convert(speechChunk.ToArray(), sourceFormat, pcm32Target);
            var backgroundConverted = Convert(backgroundChunk.ToArray(), sourceFormat, pcm32Target);

            // Mix the two audio chunks using optimized mixing
            var mixedAudio = MixPcm32AudioData(speechConverted.audioData, speechChunkVolume, backgroundConverted.audioData, backgroundChunkVolume);

            // Re-encode the mixed audio back to the original desired format.
            var mixedPcmSourceFormat = new TTSProviderAvailableAudioFormat { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = sampleRate, BitsPerSample = 32 };
            var finalTargetFormat = new AudioRequestDetails { RequestedEncoding = encodingType, RequestedSampleRateHz = sampleRate, RequestedBitsPerSample = bitsPerSample };
            var (finalMixedAudio, _) = Convert(mixedAudio, mixedPcmSourceFormat, finalTargetFormat);

            return finalMixedAudio;
        }

        #region Private Helper Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] MixPcm32AudioData(ReadOnlySpan<byte> speechData, float speechVolume, ReadOnlySpan<byte> backgroundData, float backgroundVolume)
        {
            int speechSampleCount = speechData.Length >> 2; // Divide by 4 (faster than / 4)
            int backgroundSampleCount = backgroundData.Length >> 2;
            int maxSampleCount = Math.Max(speechSampleCount, backgroundSampleCount);

            if (maxSampleCount == 0) return Array.Empty<byte>();

            byte[] mixedAudio = new byte[maxSampleCount << 2]; // Multiply by 4 (faster than * 4)

            unsafe
            {
                fixed (byte* speechPtr = speechData)
                fixed (byte* backgroundPtr = backgroundData)
                fixed (byte* mixedPtr = mixedAudio)
                {
                    int* speechSamples = (int*)speechPtr;
                    int* backgroundSamples = (int*)backgroundPtr;
                    int* mixedSamples = (int*)mixedPtr;

                    // Process samples in chunks for better cache performance
                    const int chunkSize = 16; // Process 16 samples at a time
                    int fullChunks = maxSampleCount / chunkSize;
                    int remainder = maxSampleCount % chunkSize;

                    // Process full chunks
                    for (int chunk = 0; chunk < fullChunks; chunk++)
                    {
                        int baseIndex = chunk * chunkSize;

                        for (int i = 0; i < chunkSize; i++)
                        {
                            int sampleIndex = baseIndex + i;

                            long speechSample = sampleIndex < speechSampleCount ?
                                (long)(speechSamples[sampleIndex] * speechVolume) : 0L;
                            long backgroundSample = sampleIndex < backgroundSampleCount ?
                                (long)(backgroundSamples[sampleIndex] * backgroundVolume) : 0L;

                            long mixedSample = speechSample + backgroundSample;
                            mixedSamples[sampleIndex] = (int)Math.Clamp(mixedSample, int.MinValue, int.MaxValue);
                        }
                    }

                    // Process remaining samples
                    int remainderStart = fullChunks * chunkSize;
                    for (int i = 0; i < remainder; i++)
                    {
                        int sampleIndex = remainderStart + i;

                        long speechSample = sampleIndex < speechSampleCount ?
                            (long)(speechSamples[sampleIndex] * speechVolume) : 0L;
                        long backgroundSample = sampleIndex < backgroundSampleCount ?
                            (long)(backgroundSamples[sampleIndex] * backgroundVolume) : 0L;

                        long mixedSample = speechSample + backgroundSample;
                        mixedSamples[sampleIndex] = (int)Math.Clamp(mixedSample, int.MinValue, int.MaxValue);
                    }
                }
            }

            return mixedAudio;
        }

        private static byte[] ApplyVolumeToChunk(ReadOnlySpan<byte> audioData, TTSProviderAvailableAudioFormat sourceFormat, float volume)
        {
            if (Math.Abs(volume - 1.0f) < 0.001f) // If volume is essentially 1.0, return copy
            {
                return audioData.ToArray();
            }

            // Convert to PCM 32-bit for volume processing
            var pcm32Target = new AudioRequestDetails { RequestedEncoding = AudioEncodingTypeEnum.PCM, RequestedSampleRateHz = sourceFormat.SampleRateHz, RequestedBitsPerSample = 32 };
            var converted = Convert(audioData.ToArray(), sourceFormat, pcm32Target);

            // Apply volume in place
            int sampleCount = converted.audioData.Length >> 2;
            byte[] volumeAdjusted = new byte[converted.audioData.Length];

            unsafe
            {
                fixed (byte* sourcePtr = converted.audioData)
                fixed (byte* destPtr = volumeAdjusted)
                {
                    int* sourceSamples = (int*)sourcePtr;
                    int* destSamples = (int*)destPtr;

                    for (int i = 0; i < sampleCount; i++)
                    {
                        long adjustedSample = (long)(sourceSamples[i] * volume);
                        destSamples[i] = (int)Math.Clamp(adjustedSample, int.MinValue, int.MaxValue);
                    }
                }
            }

            // Convert back to original format
            var volumeSourceFormat = new TTSProviderAvailableAudioFormat { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = sourceFormat.SampleRateHz, BitsPerSample = 32 };
            var finalTarget = new AudioRequestDetails { RequestedEncoding = sourceFormat.Encoding, RequestedSampleRateHz = sourceFormat.SampleRateHz, RequestedBitsPerSample = sourceFormat.BitsPerSample };
            var (finalAudio, _) = Convert(volumeAdjusted, volumeSourceFormat, finalTarget);

            return finalAudio;
        }

        public static IWaveProvider CreatePcm32FloatProvider(byte[] sourceAudio, TTSProviderAvailableAudioFormat sourceFormat)
        {
            IWaveProvider rawProvider;
            var sourceStream = new MemoryStream(sourceAudio);

            switch (sourceFormat.Encoding)
            {
                case AudioEncodingTypeEnum.WAV:
                    rawProvider = new WaveFileReader(new MemoryStream(FixWavHeader(sourceAudio)));
                    break;

                case AudioEncodingTypeEnum.PCM:
                    var pcmFormat = new WaveFormat(sourceFormat.SampleRateHz, sourceFormat.BitsPerSample, 1);
                    rawProvider = new RawSourceWaveStream(sourceAudio, 0, sourceAudio.Length, pcmFormat);
                    break;

                case AudioEncodingTypeEnum.MULAW:
                    {
                        var pcmShorts = _audioEncoder.DecodeAudio(sourceAudio, new AudioFormat(SDPWellKnownMediaFormatsEnum.PCMU));
                        var pcmBytesMULAW = new byte[pcmShorts.Length * sizeof(short)];
                        Buffer.BlockCopy(pcmShorts, 0, pcmBytesMULAW, 0, pcmBytesMULAW.Length);
                        var mulawWF = new WaveFormat(8000, 16, 1);
                        rawProvider = new RawSourceWaveStream(pcmBytesMULAW, 0, pcmBytesMULAW.Length, mulawWF);
                        break;
                    }

                case AudioEncodingTypeEnum.ALAW:
                    {
                        var pcmShorts = _audioEncoder.DecodeAudio(sourceAudio, new AudioFormat(SDPWellKnownMediaFormatsEnum.PCMA));
                        var pcmBytesALAW = new byte[pcmShorts.Length * sizeof(short)];
                        Buffer.BlockCopy(pcmShorts, 0, pcmBytesALAW, 0, pcmBytesALAW.Length);
                        var alawWF = new WaveFormat(8000, 16, 1);
                        rawProvider = new RawSourceWaveStream(pcmBytesALAW, 0, pcmBytesALAW.Length, alawWF);
                        break;
                    }

                case AudioEncodingTypeEnum.G722:
                    {
                        var pcmShorts = _audioEncoder.DecodeAudio(sourceAudio, new AudioFormat(SDPWellKnownMediaFormatsEnum.G722));
                        var pcmBytesG722 = new byte[pcmShorts.Length * sizeof(short)];
                        Buffer.BlockCopy(pcmShorts, 0, pcmBytesG722, 0, pcmBytesG722.Length);
                        var g722Wf = new WaveFormat(16000, 16, 1);
                        rawProvider = new RawSourceWaveStream(pcmBytesG722, 0, pcmBytesG722.Length, g722Wf);
                        break;
                    }

                case AudioEncodingTypeEnum.G729:
                    {
                        var pcmShorts = _audioEncoder.DecodeAudio(sourceAudio, new AudioFormat(SDPWellKnownMediaFormatsEnum.G729));
                        var pcmBytesG729 = new byte[pcmShorts.Length * sizeof(short)];
                        Buffer.BlockCopy(pcmShorts, 0, pcmBytesG729, 0, pcmBytesG729.Length);
                        var g729Wf = new WaveFormat(8000, 16, 1);
                        rawProvider = new RawSourceWaveStream(pcmBytesG729, 0, pcmBytesG729.Length, g729Wf);
                        break;
                    }

                case AudioEncodingTypeEnum.OPUS:
                    rawProvider = DecodeFromOpus(sourceAudio, sourceFormat.SampleRateHz);
                    break;

                case AudioEncodingTypeEnum.MPEG:
                    var inputStream = new MemoryStream(sourceAudio);
                    var builder = new Mp3FileReader.FrameDecompressorBuilder(wf => new Mp3FrameDecompressor(wf));
                    rawProvider = new Mp3FileReaderBase(inputStream, builder);
                    break;

                default:
                    throw new NotSupportedException($"Unsupported source encoding type: {sourceFormat.Encoding}");
            }

            if (rawProvider.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
            {
                if (rawProvider.WaveFormat.BitsPerSample != 32)
                {
                    return rawProvider.ToSampleProvider().ToWaveProvider();
                }

                return rawProvider;
            }
            else
            {
                return new WaveToSampleProvider(rawProvider).ToWaveProvider();
            }
        }

        private static IWaveProvider CreateResampler(IWaveProvider pcm32FloatProvider, AudioRequestDetails targetFormat)
        {
            // Input is guaranteed to be 32-bit float PCM.
            if (pcm32FloatProvider.WaveFormat.SampleRate == targetFormat.RequestedSampleRateHz && pcm32FloatProvider.WaveFormat.BitsPerSample == 32)
            {
                return pcm32FloatProvider; // No resampling needed.
            }

            var sampleProvider = pcm32FloatProvider.ToSampleProvider();
            // WdlResamplingSampleProvider is a high-quality resampler included with NAudio.
            var resampler = new WdlResamplingSampleProvider(sampleProvider, targetFormat.RequestedSampleRateHz);
            return resampler.ToWaveProvider();
        }

        private static byte[] EncodeToTargetFormat(IWaveProvider pcmProvider, AudioRequestDetails targetFormat)
        {
            using var ms = new MemoryStream();

            // The input provider is always 32-bit float PCM at the correct sample rate.
            switch (targetFormat.RequestedEncoding)
            {
                case AudioEncodingTypeEnum.PCM:
                case AudioEncodingTypeEnum.WAV:
                    // PCM/WAV encoding logic remains the same.
                    {
                        IWaveProvider finalProvider;
                        var sampleProvider = pcmProvider.ToSampleProvider();

                        switch (targetFormat.RequestedBitsPerSample)
                        {
                            case 16:
                                finalProvider = new SampleToWaveProvider16(sampleProvider);
                                break;
                            case 24:
                                finalProvider = new SampleToWaveProvider24(sampleProvider);
                                break;
                            case 32:
                                // Use our new, custom converter for 32-bit signed integer PCM.
                                finalProvider = new SampleToWaveProvider32(sampleProvider);
                                break;
                            default:
                                throw new NotSupportedException($"Encoding to {targetFormat.RequestedBitsPerSample}-bit PCM is not supported.");
                        }

                        if (targetFormat.RequestedEncoding == AudioEncodingTypeEnum.WAV)
                        {
                            using var writer = new WaveFileWriter(new IgnoreDisposeStream(ms), finalProvider.WaveFormat);
                            finalProvider.CopyTo(writer);
                        }
                        else
                        {
                            finalProvider.CopyTo(ms);
                        }
                        return ms.ToArray();
                    }

                // NEW UNIFIED ENCODING LOGIC:
                case AudioEncodingTypeEnum.MULAW:
                case AudioEncodingTypeEnum.ALAW:
                case AudioEncodingTypeEnum.G722:
                case AudioEncodingTypeEnum.G729:
                    {
                        // SIPSorcery encoder expects 16-bit PCM shorts.
                        // First, convert our float provider to 16-bit PCM.
                        var pcm16Provider = new SampleToWaveProvider16(pcmProvider.ToSampleProvider());

                        // Verify sample rates before encoding.
                        var expectedRate = GetExpectedSampleRate(targetFormat.RequestedEncoding);
                        if (pcm16Provider.WaveFormat.SampleRate != expectedRate)
                        {
                            throw new InvalidOperationException($"{targetFormat.RequestedEncoding} requires a {expectedRate} Hz sample rate, but received {pcm16Provider.WaveFormat.SampleRate} Hz.");
                        }

                        var pcmBytes = ReadAllBytes(pcm16Provider);
                        var pcmShorts = new short[pcmBytes.Length / 2];
                        Buffer.BlockCopy(pcmBytes, 0, pcmShorts, 0, pcmBytes.Length);

                        var audioFormat = new AudioFormat(GetSdpFormat(targetFormat.RequestedEncoding));
                        var encodedBytes = _audioEncoder.EncodeAudio(pcmShorts, audioFormat);
                        return encodedBytes;
                    }

                case AudioEncodingTypeEnum.OPUS:
                    var pcm16ProviderOpus = new SampleToWaveProvider16(pcmProvider.ToSampleProvider());
                    return EncodeToOpus(pcm16ProviderOpus, targetFormat);

                case AudioEncodingTypeEnum.MPEG:
                    throw new NotSupportedException($"Encoding to {targetFormat.RequestedEncoding} is not supported.");

                default:
                    throw new ArgumentException($"Unsupported target encoding type: {targetFormat.RequestedEncoding}");
            }
        }

        private static byte[] ApplyVolumeToChunk(byte[] chunk, TTSProviderAvailableAudioFormat sourceFormat, float volume)
        {
            if (Math.Abs(volume - 1.0f) < 0.001) return chunk;

            var speech32BitProvider = CreatePcm32FloatProvider(chunk.ToArray(), sourceFormat).ToSampleProvider();
            var speechVolumeProvider = new VolumeSampleProvider(speech32BitProvider);
            speechVolumeProvider.Volume = volume;

            var mixedMemoryStream = new MemoryStream();
            float[] buffer = new float[speechVolumeProvider.WaveFormat.AverageBytesPerSecond];
            int bytesRead;
            while ((bytesRead = speechVolumeProvider.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < bytesRead / 4; i++)
                {
                    var sample = buffer[i];
                    sample = Math.Max(-1.0f, Math.Min(1.0f, sample));
                    var pcmSample = (int)(sample * int.MaxValue);
                    var pcmBytes = BitConverter.GetBytes(pcmSample);
                    mixedMemoryStream.Write(pcmBytes, 0, pcmBytes.Length);
                }
            }

            // Re-encode back to the original target format
            var pcmSourceFormat = new TTSProviderAvailableAudioFormat { Encoding = AudioEncodingTypeEnum.PCM, SampleRateHz = sourceFormat.SampleRateHz, BitsPerSample = 32 };
            var finalTargetFormat = new AudioRequestDetails { RequestedEncoding = sourceFormat.Encoding, RequestedSampleRateHz = sourceFormat.SampleRateHz, RequestedBitsPerSample = sourceFormat.BitsPerSample };
            var (finalAudio, _) = Convert(mixedMemoryStream.ToArray(), pcmSourceFormat, finalTargetFormat);

            return finalAudio;
        }

        private static IWaveProvider DecodeFromOpus(byte[] opusData, int sampleRate)
        {
            var decoder = OpusCodecFactory.CreateDecoder(sampleRate, 1);
            int maxFrameSize = sampleRate * 120 / 1000;
            var pcmBuffer = new short[maxFrameSize * 1]; // 1 channel
            int samplesDecoded = decoder.Decode(opusData, pcmBuffer, maxFrameSize, false);
            var pcmBytes = new byte[samplesDecoded * sizeof(short)];
            Buffer.BlockCopy(pcmBuffer, 0, pcmBytes, 0, pcmBytes.Length);
            var waveFormat = new WaveFormat(sampleRate, 16, 1);
            return new RawSourceWaveStream(pcmBytes, 0, pcmBytes.Length, waveFormat);
        }

        private static byte[] EncodeToOpus(IWaveProvider pcmProvider, AudioRequestDetails targetFormat)
        {
            int sampleRate = pcmProvider.WaveFormat.SampleRate;
            int frameSizeMs = 20; // Standard frame size
            int frameSizeSamples = sampleRate * frameSizeMs / 1000;

            var encoder = OpusCodecFactory.CreateEncoder(sampleRate, 1, Concentus.Enums.OpusApplication.OPUS_APPLICATION_VOIP);
            encoder.Bitrate = 32000;

            using var ms = new MemoryStream();
            var pcmShortBuffer = new short[frameSizeSamples];
            var readByteBuffer = new byte[frameSizeSamples * sizeof(short)];
            int bytesRead;

            while ((bytesRead = pcmProvider.Read(readByteBuffer, 0, readByteBuffer.Length)) > 0)
            {
                Buffer.BlockCopy(readByteBuffer, 0, pcmShortBuffer, 0, bytesRead);
                int samplesInFrame = bytesRead / sizeof(short);
                var encodedPacket = new byte[4000];
                int encodedLength = encoder.Encode(
                    pcmShortBuffer.AsSpan(0, samplesInFrame),
                    samplesInFrame,
                    encodedPacket.AsSpan(),
                    encodedPacket.Length
                );

                ms.Write(encodedPacket, 0, encodedLength);
            }
            return ms.ToArray();
        }

        private static byte[] ReadAllBytes(IWaveProvider provider)
        {
            using var ms = new MemoryStream();
            provider.CopyTo(ms);
            return ms.ToArray();
        }

        private static bool IsWavFile(byte[] data)
        {
            if (data == null || data.Length < 12) return false;
            return data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F' &&
                   data[8] == 'W' && data[9] == 'A' && data[10] == 'V' && data[11] == 'E';
        }

        private static byte[] FixWavHeader(byte[] wavData)
        {
            if (!IsWavFile(wavData) || wavData.Length < 44) return wavData;

            int correctFileSize = wavData.Length - 8;
            Array.Copy(BitConverter.GetBytes(correctFileSize), 0, wavData, 4, 4);

            int dataChunkOffset = -1;
            for (int i = 12; i < wavData.Length - 8; i++)
            {
                if (wavData[i] == 'd' && wavData[i + 1] == 'a' && wavData[i + 2] == 't' && wavData[i + 3] == 'a')
                {
                    dataChunkOffset = i;
                    break;
                }
            }

            if (dataChunkOffset != -1)
            {
                int dataChunkSizeOffset = dataChunkOffset + 4;
                int correctDataSize = wavData.Length - (dataChunkOffset + 8);
                Array.Copy(BitConverter.GetBytes(correctDataSize), 0, wavData, dataChunkSizeOffset, 4);
            }
            return wavData;
        }

        private static SDPWellKnownMediaFormatsEnum GetSdpFormat(AudioEncodingTypeEnum encoding)
        {
            switch (encoding)
            {
                case AudioEncodingTypeEnum.MULAW: return SDPWellKnownMediaFormatsEnum.PCMU;
                case AudioEncodingTypeEnum.ALAW: return SDPWellKnownMediaFormatsEnum.PCMA;
                case AudioEncodingTypeEnum.G722: return SDPWellKnownMediaFormatsEnum.G722;
                case AudioEncodingTypeEnum.G729: return SDPWellKnownMediaFormatsEnum.G729;
                default: throw new NotSupportedException($"No Sdp format mapping for {encoding}");
            }
        }

        private static int GetExpectedSampleRate(AudioEncodingTypeEnum encoding)
        {
            switch (encoding)
            {
                case AudioEncodingTypeEnum.MULAW:
                case AudioEncodingTypeEnum.ALAW:
                case AudioEncodingTypeEnum.G729:
                    return 8000;
                case AudioEncodingTypeEnum.G722:
                    return 16000;
                default:
                    throw new NotSupportedException($"No expected sample rate for {encoding}");
            }
        }


        #endregion

    }

    internal static class WaveProviderExtensions
    {
        public static void CopyTo(this IWaveProvider source, Stream destination)
        {
            byte[] buffer = new byte[source.WaveFormat.AverageBytesPerSecond];
            int bytesRead;
            while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                destination.Write(buffer, 0, bytesRead);
            }
        }
    }

    internal class SampleToWaveProvider32 : IWaveProvider
    {
        private readonly ISampleProvider _sourceProvider;
        private readonly WaveFormat _waveFormat;
        private readonly float[] _sourceBuffer;

        public WaveFormat WaveFormat => _waveFormat;

        public SampleToWaveProvider32(ISampleProvider sourceProvider)
        {
            if (sourceProvider.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                throw new ArgumentException("Source provider must be 32-bit float.");
            if (sourceProvider.WaveFormat.BitsPerSample != 32)
                throw new ArgumentException("Source provider must be 32-bit float.");

            _sourceProvider = sourceProvider;
            _waveFormat = new WaveFormat(sourceProvider.WaveFormat.SampleRate, 32, sourceProvider.WaveFormat.Channels);

            // Use a buffer for efficiency
            _sourceBuffer = new float[sourceProvider.WaveFormat.SampleRate];
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            // The number of 32-bit integer samples we need to generate
            int samplesRequired = count / 4;

            // The number of float samples we need to read from the source
            int samplesToRead = Math.Min(samplesRequired, _sourceBuffer.Length);

            // Read float samples from the source provider
            int samplesActuallyRead = _sourceProvider.Read(_sourceBuffer, 0, samplesToRead);

            int outIndex = offset;
            for (int i = 0; i < samplesActuallyRead; i++)
            {
                // Convert float sample (-1.0 to 1.0) to 32-bit integer sample
                float floatSample = _sourceBuffer[i];

                // Clamp the sample to prevent overflow
                floatSample = Math.Max(-1.0f, Math.Min(1.0f, floatSample));

                int pcmSample = (int)(floatSample * 2147483647.0);

                // Copy the bytes of the integer sample into the output buffer
                byte[] sampleBytes = BitConverter.GetBytes(pcmSample);
                Buffer.BlockCopy(sampleBytes, 0, buffer, outIndex, 4);

                outIndex += 4;
            }

            // Return the total number of bytes written to the output buffer
            return samplesActuallyRead * 4;
        }
    }
}